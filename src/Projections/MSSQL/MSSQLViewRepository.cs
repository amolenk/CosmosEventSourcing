using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Projections.MSSQL.DbSchema;

namespace Projections.MSSQL
{
    public class MSSQLViewRepository : IViewRepository
    {
        private esdemo3Context _context;

        public MSSQLViewRepository(
            esdemo3Context context)
        {
            _context = context;
        }

        public async Task<IView> LoadViewAsync(string name)
        {
            if (!name.Contains(':'))
            {
                return null;
            }

            string tableName = name.Substring(0, name.IndexOf(':'));
            string rowId = name.Substring(name.IndexOf(':') + 1);

            switch (tableName.Trim().ToLower())
            {
                case "meter":
                    return await GetMeterView(rowId);
                default:
                    return new MSSQLView();
            }
        }

        private async Task<IView> GetMeterView(string rowId)
        {
            var row = await _context.Meters.AsNoTracking().SingleOrDefaultAsync(x => x.MeterId == rowId);
            if (row == null)
            {
                return new MSSQLView();
            }

            var view = new MSSQLView(
                new ViewCheckpoint()
                {
                    LogicalSequenceNumber = (long) row.LogicalCheckPointLsn,
                    ItemIds = row.LogicalCheckPointItemIds.Split(",").ToList()
                }, JObject.FromObject(row));
            return view;
        }

        public async Task<bool> SaveViewAsync(string name, IView view)
        {
            var sqlView = (MSSQLView) view;
            if (!name.Contains(':'))
            {
                return false;
            }

            string tableName = name.Substring(0, name.IndexOf(':'));
            string rowId = name.Substring(name.IndexOf(':') + 1);

            var meter = view.Payload.ToObject<Meter>();
            
            // add 2 fields that are used to track 'Handled' status
            meter.LogicalCheckPointLsn = sqlView.LogicalCheckpoint.LogicalSequenceNumber;
            meter.LogicalCheckPointItemIds = string.Join(',', sqlView.LogicalCheckpoint.ItemIds);

            if (sqlView.IsNew)
            {
                _context.Meters.Add(meter);
            }
            else
            {
                _context.Meters.Update(meter);
            }

            return await _context.SaveChangesAsync() > 0;
        }
    }
}