using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore;

namespace Projections
{
    public interface IViewRepository
    {
        Task<IView> LoadViewAsync(string name);

        Task<bool> SaveViewAsync(string name, IView view);
    }
}