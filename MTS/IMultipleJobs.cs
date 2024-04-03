using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTS
{
    internal interface IMultipleJobs
    {
        void Run(string JobId);
        DataTable Polling();
    }
}
