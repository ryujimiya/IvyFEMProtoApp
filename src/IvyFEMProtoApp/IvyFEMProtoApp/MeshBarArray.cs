﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IvyFEM
{
    class MeshBarArray
    {
        public uint Id { get; set; }
        public uint ECadId { get; set; }
        public uint[] SEId { get; } = new uint[2];
        public uint[] LRId { get; } = new uint[2];
        public int Layer { get; set; }
        public IList<MeshBar> Bars { get; } = new List<MeshBar>();
    }
}
