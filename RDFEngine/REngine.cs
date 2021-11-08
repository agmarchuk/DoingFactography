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
            Dictionary<string, List<RInverseLink>> inverseDic = new Dictionary<string, List<RInverseLink>>();
            Action<string, RInverseLink> AddInverse = (id, ilink) =>
            {
                if (!inverseDic.ContainsKey(id)) inverseDic.Add(id, new List<RInverseLink>());
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
                            AddInverse(resource.Value, new RInverseLink() { Prop = prop, Source = nodeId }); // ДОБАВЛЕНИЕ
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
                if (!rdatabase.ContainsKey(id)) continue;
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
        public void UpdateRRecord(RRecord record, string forbidden, string modelId, bool delete)
        {
            if (rdatabase.ContainsKey(record.Id))
            {
                if (delete)
                {
                    foreach (var prop in rdatabase[record.Id].Props)
                    {
                        if (prop is RLink)
                        {
                            rdatabase[((RLink)prop).Resource].Props = rdatabase[((RLink)prop).Resource].Props
                                .Where(x => (x is RField) || (x is RInverseLink && ((RInverseLink)x).Source != record.Id))
                                .ToArray();
                        }
                    }
                    rdatabase.Remove(record.Id);
                }
                else
                {
                    rdatabase[record.Id] = generateRecordToAdd(record, forbidden, modelId);
                }
            }
            else
            {
                rdatabase.Add(record.Id, generateRecordToAdd(record, forbidden, modelId));
                var modelInverse = new RInverseLink();
                modelInverse.Prop = forbidden;
                modelInverse.Source = record.Id;
                var linkId = ((RDirect)record.Props.FirstOrDefault(x => (x is RDirect) && (x.Prop != forbidden))).DRec.Id;
                var linkInverse = new RInverseLink();
                linkInverse.Prop = record.Props.FirstOrDefault(x => (x is RDirect) && (x.Prop != forbidden)).Prop;
                linkInverse.Source = record.Id;
                rdatabase[modelId].Props = rdatabase[modelId].Props.Append(modelInverse).ToArray();
                rdatabase[linkId].Props = rdatabase[linkId].Props.Append(linkInverse).ToArray();
            }
        }
        public void UpdateRRecord(RRecord record, string forbidden, string modelId)
        {
            UpdateRRecord(record, forbidden, modelId, false);
        }
        private RRecord generateRecordToAdd(RRecord record, string forbidden, string modelId)
        {
            RRecord toAdd = new RRecord();
            toAdd.Id = record.Id;
            toAdd.Tp = record.Tp;
            toAdd.Props = new RProperty[record.Props.Length];
            for (int i = 0; i < record.Props.Length; i++)
            {
                var prop = record.Props[i];
                if (prop is RField)
                {
                    toAdd.Props[i] = prop;
                }
                if (prop is RDirect)
                {
                    if (prop.Prop == forbidden)
                    {
                        RLink link = new RLink();
                        link.Prop = prop.Prop;
                        link.Resource = modelId;
                        toAdd.Props[i] = link;
                    }
                    else
                    {
                        RLink link = new RLink();
                        link.Prop = prop.Prop;
                        link.Resource = ((RDirect)prop).DRec.Id;
                        toAdd.Props[i] = link;
                    }

                }

            }
            return toAdd;
        }
        public IEnumerable<RRecord> RSearch(string searchstring)
        {
            searchstring = searchstring.ToLower();
            return rdatabase
                .Select(pair => pair.Value)
                .Where(rr => 
                {
                    return rr.Props.Any(p => p is RField && ((RField)p).Prop == "name" && ((RField)p).Value.ToLower().StartsWith(searchstring)); 
                });
        }

        public IEnumerable<RRecord> RSearch(string searchstring, string type)
        {
            searchstring = searchstring.ToLower();
            return rdatabase
                .Select(pair => pair.Value)
                .Where(rr => rr.Tp == type)
                .Where(rr =>
                {
                    return rr.Props.Any(p => p is RField && ((RField)p).Prop == "name" && ((RField)p).Value.ToLower().StartsWith(searchstring));
                });
        }

        // ==== Определения, созданные для Portrait2, Portrait3


        public RRecord BuildPortrait(string id)
        {
            return BuPo(id, 2, null);
        }
        private RRecord BuPo(string id, int level, string forbidden)
        {
            var rec = GetRRecord(id);
            if (rec == null) return null;
            RRecord result_rec = new RRecord()
            {
                Id = rec.Id,
                Tp = rec.Tp,
                Props = rec.Props.Select<RProperty, RProperty>(p =>
                {
                    if (p is RField)
                        return new RField() { Prop = p.Prop, Value = ((RField)p).Value };
                    else if (level > 0 && p is RLink && p.Prop != forbidden)
                        return new RDirect() { Prop = p.Prop, DRec = BuPo(((RLink)p).Resource, 0, null) };
                    else if (level > 1 && p is RInverseLink)
                        return new RInverse() { Prop = p.Prop, IRec = BuPo(((RInverseLink)p).Source, 1, p.Prop) };
                    return null;
                }).Where(p => p != null).ToArray()
            };
            return result_rec;
        }
    }
}
