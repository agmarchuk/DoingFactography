using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RDFEngine;

namespace MagBlazor.Models
{
    public class GeneTreeModel
    {
        public RRecord node;
        public RRecord spouse;
        public GeneTreeModel parent;
        public GeneTreeModel[] childs; 
    }
    public class RRecordLevel
    {
        public RRecord record;
        public RRecord spouse;
        public int level;

        private static IEnumerable<RRecordLevel> Tr(GeneTreeModel model, int level)
        {
            var firstelem = new RRecordLevel { record = model.node, spouse = model.spouse, level = level };
            var query2 = (new RRecordLevel[] { firstelem })
                .Concat(model.childs.SelectMany(ch => Tr(ch, level + 1)));
            var query3 = query2.ToArray();
            return query3;
        }
        public static RRecordLevel[] Traverse(GeneTreeModel model)
        {
            return Tr(model, 0).ToArray();
        }
    }
}
