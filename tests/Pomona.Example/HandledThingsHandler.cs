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
using System.Linq;

using Pomona.Example.Models;

namespace Pomona.Example
{
    public class HandledThingsHandler
    {
        private readonly CritterRepository repository;


        public HandledThingsHandler(CritterRepository repository)
        {
            if (repository == null)
                throw new ArgumentNullException("repository");
            this.repository = repository;
        }


        public void Delete(HandledThing handledThing)
        {
            this.repository.Delete(handledThing);
        }

        public IQueryable<HandledThing> Query(int id)
        {
            return this.repository.Query<HandledThing>();
        }

        public HandledThing Get(int id)
        {
            var thing = this.repository.Query<HandledThing>().First(x => x.Id == id);
            thing.FetchedCounter++;
            return thing;
        }


        public HandledThing Patch(HandledThing handledThing)
        {
            handledThing.PatchCounter++;
            return this.repository.Save(handledThing);
        }


        public HandledThing Post(HandledThing handledThing, PomonaRequest request)
        {
            if (handledThing == null)
                throw new ArgumentNullException("handledThing");
            if (request == null)
                throw new ArgumentNullException("request");
            handledThing.Marker = "HANDLER WAS HERE!";
            return (HandledThing)this.repository.Post(handledThing);
        }
    }
}