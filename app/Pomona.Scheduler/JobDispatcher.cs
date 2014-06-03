﻿#region License

// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright © 2014 Karsten Nikolai Strand
// 
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// ----------------------------------------------------------------------------

#endregion

using System;

using Pomona.Common.Web;

namespace Pomona.Scheduler
{
    public class JobDispatcher : IJobDispatcher
    {
        private readonly IJobStore jobStore;
        private readonly IWebClient webClient;


        public JobDispatcher(IJobStore jobStore)
        {
            if (jobStore == null)
                throw new ArgumentNullException("jobStore");
            this.jobStore = jobStore;
            this.webClient = new HttpWebRequestClient();
        }


        public bool Tick()
        {
            IJob job;
            if (this.jobStore.TryDequeue(out job))
            {
                RunJob(job);
                return true;
            }
            return false;
        }


        private void RunJob(IJob job)
        {
            var response = this.webClient.Send(new WebClientRequestMessage(job.Url, null, job.Method));
            var statusCode = (int)response.StatusCode;
            if (statusCode - (statusCode % 100) != 200)
                throw new NotImplementedException("TODO: Implement error handling and retrying.");

            this.jobStore.Complete(job);
        }
    }
}