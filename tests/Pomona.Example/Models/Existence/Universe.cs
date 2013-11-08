﻿#region License

// ----------------------------------------------------------------------------
// Pomona source code
// 
// Copyright © 2013 Karsten Nikolai Strand
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

using System.Collections.Generic;

namespace Pomona.Example.Models.Existence
{
    // /galaxies/milkyway/planetary-systems/solar/planets/pluto/moons/earthmoon

    public abstract class CelestialObject : EntityBase
    {
        public string Name { get; set; }
    }

    public class Galaxy : CelestialObject
    {
        private ICollection<PlanetarySystem> planetarySystems = new List<PlanetarySystem>();

        public ICollection<PlanetarySystem> PlanetarySystems { get { return planetarySystems; }}
    }

    public class PlanetarySystem : CelestialObject
    {
        public Galaxy Galaxy { get; set; }

        private ICollection<Planet> planets = new List<Planet>();

        public ICollection<Planet> Planets
        {
            get { return this.planets; }
        }
    }

    public class Planet : CelestialObject
    {
        private ICollection<Moon> moons = new List<Moon>();

        public ICollection<Moon> Moons
        {
            get { return this.moons; }
        }

        public PlanetarySystem PlanetarySystem { get; set; }
    }

    public class Moon : CelestialObject
    {
    }
}