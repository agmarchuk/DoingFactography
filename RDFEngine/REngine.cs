﻿using System;
using System.Collections.Generic;
using System.Linq;
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
            // ДОБАВЛЕНИЕ обратных ссылок
            Dictionary<string, List<RInverse>> inverseDic = new Dictionary<string, List<RInverse>>();
            Action<string, RInverse> AddInverse = (id, ilink) =>
            {
                if (!inverseDic.ContainsKey(id)) inverseDic.Add(id, new List<RInverse>());
                inverseDic[id].Add(ilink);
            };

            rdatabase = records.Select(x =>
            {
                string nodeId = x.Attribute(IEngine.rdfabout).Value;
                return new RRecord()
                {
                    Id = nodeId,
                    Tp = x.Name.NamespaceName + x.Name.LocalName,
                    Props = x.Elements().Select<XElement, RProperty>(el =>
                    {
                        string prop = el.Name.NamespaceName + el.Name.LocalName;
                        XAttribute resource = el.Attribute(IEngine.rdfresource);
                        if (resource == null)
                        {  // Поле   TODO: надо учесть языковый спецификатор, а может, и тип
                            return new RField() { Prop = prop, Value = el.Value };
                        }
                        else
                        {  // ссылка
                            // Создадим RInverse и добавим в inverseDic 
                            AddInverse(resource.Value, new RInverse() { Prop = prop, Source = nodeId }); // ДОБАВЛЕНИЕ
                            return new RLink() { Prop = prop, Resource = resource.Value };
                        }
                    }).ToArray()
                };
            }).ToDictionary(rr => rr.Id);

            // ДОБАВЛЕНИЕ Вставим наработанные обратные ссылки
            foreach (var pair in inverseDic)
            {
                string id = pair.Key;
                var list = pair.Value;
                var node = rdatabase[id];
                node.Props = node.Props.Concat(list).ToArray();
            }
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
                    return rr.Props.Any(p => p is RField && ((RField)p).Prop == "name" && ((RField)p).Value.ToLower().StartsWith(searchstring)); 
                });
        }
    }
}
