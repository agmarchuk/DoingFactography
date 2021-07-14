using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RDFEngine
{
    // Движок, основанный на объектном R-представлении
    public class REngine : IEngine
    {
        // База данных будет:
        private IDictionary<string, RRecord> rdatabase;
        public void Load(IEnumerable<XElement> records)
        {
            rdatabase = records.Select(x => new RRecord()
            {
                Id = x.Attribute(IEngine.rdfabout).Value,
                Tp = x.Name.NamespaceName + x.Name.LocalName,
                Props = x.Elements().Select<XElement, RProperty>(el =>
                {
                    string prop = el.Name.NamespaceName + el.Name.LocalName;
                    XAttribute resource = el.Attribute(IEngine.rdfresource);
                    if (records == null)
                    {  // Поле   TODO: надо учесть языковый спецификатор, а может, и тип
                        return new RField() { Prop = prop, Value = el.Value };
                    }
                    else
                    {  // ссылка
                        return new RLink() { Prop = prop, Resource = resource.Value };
                    }
                }).ToArray()
            }).ToDictionary(rr => rr.Id);
        }

        public void Build()
        {
            // Ничего делать не надо, все сделано при загрузке
        }

        public void Clear()
        {
            rdatabase.Clear();
        }

        public RRecord GetRRecord(string id)
        {
            RRecord rr;
            if (rdatabase.TryGetValue(id, out rr))
            {
                return rr;
            }
            return null;
        }


        public IEnumerable<RRecord> RSearch(string searchstring)
        {
            return rdatabase
                .Select(pair => pair.Value)
                .Where(rr => 
                {
                    rr.Props.FirstOrDefault(p => p is RField && ((RField)p).Prop == "http://fogid.net/o/name");
                    return true;
                });
        }
    }
}
