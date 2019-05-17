using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore;

namespace Projections
{
    public interface IViewRepository
    {
        Task<View> LoadViewAsync(string name);

        Task<bool> SaveViewAsync(string name, View view);
    }
}