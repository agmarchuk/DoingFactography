﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Polar.DB;
using Polar.OModel;

namespace OAData.Adapters
{
    public class OmAdapter : DAdapter
    {
        // Адаптер состоит из последовательности записей и пары индексов
        private UniversalSequence records;
        private SVectorIndex names;
        private PType tp_prop;
        private PType tp_rec;

        // Констуктор инициализирует то, что можно и не изменяется
        public OmAdapter()
        {
            tp_prop = new PTypeUnion(
                new NamedType("novariant", new PType(PTypeEnumeration.none)),
                new NamedType("field", new PTypeRecord(
                    new NamedType("prop", new PType(PTypeEnumeration.sstring)),
                    new NamedType("value", new PType(PTypeEnumeration.sstring)),
                    new NamedType("lang", new PType(PTypeEnumeration.sstring)))),
                new NamedType("objprop", new PTypeRecord(
                    new NamedType("prop", new PType(PTypeEnumeration.sstring)),
                    new NamedType("link", new PType(PTypeEnumeration.sstring))))
                );
            tp_rec = new PTypeRecord(
                new NamedType("id", new PType(PTypeEnumeration.sstring)),
                new NamedType("tp", new PType(PTypeEnumeration.sstring)),
                new NamedType("props", new PTypeSequence(tp_prop)));
        }

        // Главный инициализатор. Используем connectionstring 
        private string dbfolder;
        private int file_no = 0;
        public override void Init(string connectionstring)
        {
            if (connectionstring != null && connectionstring.StartsWith("om:"))
            {
                dbfolder = connectionstring.Substring("om:".Length);
            }
            if (File.Exists(dbfolder + "0.bin")) firsttime = false;
            Func<Stream> GenStream = () =>
                new FileStream(dbfolder + (file_no++) + ".bin", FileMode.OpenOrCreate, FileAccess.ReadWrite);

            // Полключение к базе данных
            Func<object, int> hashId = obj => Hashfunctions.HashRot13(((string)(((object[])obj)[0])));
            Comparer<object> comp_direct = Comparer<object>.Create(new Comparison<object>((object a, object b) =>
            {
                string val1 = (string)((object[])a)[0];
                string val2 = (string)((object[])b)[0];
                return string.Compare(val1, val2, StringComparison.OrdinalIgnoreCase);
            }));

            // Создаем последовательность R-записей
            records = new UniversalSequence(tp_rec, GenStream, null, null,
                obj => Hashfunctions.HashRot13((string)(((object[])obj)[0])),
                comp_direct,
                new HashComp[]
                { 
                    //new HashComp { Hash = null, Comp = comp_direct } 
                });
            names = new SVectorIndex(GenStream, records, obj =>
            {
                object[] props = (object[])((object[])obj)[2];
                var query = props.Where(p => (int)((object[])p)[0] == 1)
                    .Select(p => ((object[])p)[1])
                    .Cast<object[]>()
                    .Where(f => (string)f[0] == "http://fogid.net/o/name")
                    .Select(f => (string)f[1]).ToArray();
                return query;
            });

        }


        public override void StartFillDb(Action<string> turlog)
        {
            records.Clear();
            // Индексы чистятся внутри
        }
        public override void FinishFillDb(Action<string> turlog)
        {
            records.Build();
        }

        // Загузка потока x-элементов
        public override void LoadXFlow(IEnumerable<XElement> xflow, Dictionary<string, string> orig_ids)
        {
            IEnumerable<object> flow = xflow.Select(record =>
            {
                string id = record.Attribute(ONames.rdfabout).Value;
                // Корректируем идентификатор
                if (orig_ids.TryGetValue(id, out string idd)) id = idd;

                string rec_type = ONames.fog + record.Name.LocalName;
                object[] orecord = new object[] {
                        id,
                        rec_type,
                        record.Elements()
                            .Select(el =>
                            {
                                string prop = el.Name.NamespaceName + el.Name.LocalName;
                                if (el.Attribute(ONames.rdfresource) != null)
                                {
                                    string resource = el.Attribute(ONames.rdfresource).Value;
                                    if (orig_ids.TryGetValue(resource, out string res)) if (res != null) resource = res;
                                    return new object[] { 2, new object[] { prop, resource } };
                                }
                                else
                                {
                                    var xlang = el.Attribute("{http://www.w3.org/XML/1998/namespace}lang");
                                    string lang = xlang == null ? "" : xlang.Value;
                                    return new object[] { 1, new object[] { prop, el.Value, lang } };
                                }
                            })
                            .ToArray()
                        };
                return orecord;
            });
            records.Load(flow);
        }

        public override IEnumerable<XElement> GetAll()
        {
            throw new NotImplementedException();
        }

        public override XElement GetItemById(string id, XElement format)
        {
            throw new NotImplementedException();
        }

        public override XElement GetItemByIdBasic(string id, bool addinverse)
        {
            var rec = records.GetByKey(new object[] { id, null, null });
            if (rec == null) return null;
            XElement xres = ORecToXRec((object[])rec);
            // if (addinverse) throw new Exception("Err in GetItemByIdBasic: no implementation of addinverse");
            return xres;
        }
        private XElement ORecToXRec(object[] ob)
        {
            return new XElement("record",
                new XAttribute("id", ob[0]), new XAttribute("type", ob[1]),
                ((object[])ob[2]).Cast<object[]>().Select(uni =>
                {
                    if ((int)uni[0] == 1)
                    {
                        object[] p = (object[])uni[1];
                        XAttribute langatt = string.IsNullOrEmpty((string)p[2]) ? null : new XAttribute(ONames.xmllang, p[2]);
                        return new XElement("field", new XAttribute("prop", p[0]), langatt,
                            p[1]);
                    }
                    else if ((int)uni[0] == 2)
                    {
                        object[] p = (object[])uni[1];
                        return new XElement("direct", new XAttribute("prop", p[0]));
                    }
                    return null;
                }));
        }
        public override IEnumerable<XElement> SearchByName(string searchstring)
        {
            var query = names.LikeBySKey(searchstring).ToArray();
            return query.Cast<object[]>().Select(ob => new XElement("record",
                new XAttribute("id", ob[0]), new XAttribute("type", ob[1]),
                ((object[])ob[2]).Cast<object[]>().Select(uni =>
                {
                    if ((int)uni[0] == 1)
                    {
                        object[] p = (object[])uni[1];
                        XAttribute langatt = string.IsNullOrEmpty((string)p[2]) ? null : new XAttribute(ONames.xmllang, p[2]);
                        return new XElement("field", new XAttribute("prop", p[0]), langatt,
                            p[1]);
                    }
                    else if ((int)uni[0] == 2)
                    {
                        object[] p = (object[])uni[1];
                        return new XElement("direct", new XAttribute("prop", p[0]));
                    }
                    return null;
                })));
        }



        public override XElement PutItem(XElement record)
        {
            throw new NotImplementedException();
        }
        public override XElement Delete(string id)
        {
            throw new NotImplementedException();
        }

        public override void Close()
        {
            throw new NotImplementedException();
        }

    }
}
