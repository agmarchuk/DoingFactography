using System.Collections.Generic;
using System.Linq;
using RDFEngine;

namespace MagBlazor.Models
{
    public class PortraitModel4
    {
        public string Id, Tp, tp, name, fd, td, description, uri, doccontent;
        public string dates { get { return (fd == null ? "" : fd) + (string.IsNullOrEmpty(td) ? "" : "-" + td); } }
        public bool isorg = false;
        public RRecord[] titles = null;
        public RRecord[] works = null;
        public RRecord[] notworks = null;
        public RRecord[] livings = null;
        public RRecord[] participants = null;
        public RRecord[] indoc_parts = null; // от документа к частям
        //public RRecord[] item_parts = null; // от части к общим документам (может только 1?)
        public RRecord[] indoc_reflections = null; // от документа к отражениям
        public string todocument = null; // отказался от предыдущего поля в пользу конкретных Id направлений
        public string prev = null;
        public string next = null;

        public PortraitModel4(RRecord rec, string eid)
        {
            if (eid != null)
            {
                todocument = eid;
                var eeid = (new RXEngine()).GetRRecord(eid, true);
                var docwithparts = BuildTree(eid, new RRecord
                {
                    Props = new RProperty[]
                    {
                        new RInverse { Prop = "http://fogid.net/o/inDocument", IRec = new RRecord { Props = new RProperty[]
                        {
                            new RField { Prop = "http://fogid.net/o/pageNumbers" },
                            new RDirect { Prop = "http://fogid.net/o/partItem" }
                        }} }
                    }
                });
                string[] part_list = new string[0];
                part_list = docwithparts.Props.Cast<RInverse>()
                    .OrderBy(prt => prt.IRec.GetField("http://fogid.net/o/pageNumbers"))
                    .Select(prt => prt.IRec.GetDirectResource("http://fogid.net/o/partItem"))
                    .ToArray();
                int ind = System.Array.IndexOf(part_list, rec.Id);
                if (ind >= 0)
                {
                    if (ind - 1 >= 0) prev = part_list[ind - 1];
                    if (ind + 1 < part_list.Count()) next = part_list[ind + 1];
                }
            }
            RRecord format = new RRecord
            {
                Props = new RProperty[]
                {
                    new RField { Prop = "http://fogid.net/o/name" },
                    new RField { Prop = "http://fogid.net/o/from-date" },
                    new RField { Prop = "http://fogid.net/o/to-date" },
                    new RField { Prop = "http://fogid.net/o/description" },
                    new RField { Prop = "http://fogid.net/o/uri" },
                    new RField { Prop = "http://fogid.net/o/doc-content" },
                    new RInverse { Prop = "http://fogid.net/o/has-title" },
                    new RInverse { Prop = "http://fogid.net/o/participant" },
                    new RInverse { Prop = "http://fogid.net/o/something", IRec = new RRecord { Props = new RProperty[] 
                        {
                            new RField { Prop = "http://fogid.net/o/location-category" },
                            new RDirect { Prop = "http://fogid.net/o/location-place" }
                        } } },
                    new RInverse { Prop = "http://fogid.net/o/in-org", IRec = new RRecord { Props = new RProperty[]
                        {
                            new RField { Prop = "http://fogid.net/o/from-date" },
                            new RField { Prop = "http://fogid.net/o/role-classification" },
                            new RField { Prop = "http://fogid.net/o/role" },
                            new RDirect { Prop = "http://fogid.net/o/participant" }
                        }  } },
                    new RInverse { Prop = "http://fogid.net/o/inDocument", IRec = new RRecord { Props = new RProperty[]
                        {
                            new RField { Prop = "http://fogid.net/o/pageNumbers" },
                            new RDirect { Prop = "http://fogid.net/o/partItem"  }
                        }  } },
                    //new RInverse { Prop = "http://fogid.net/o/partItem", IRec = new RRecord { Props = new RProperty[]
                    //    {
                    //        new RField { Prop = "http://fogid.net/o/pageNumbers" },
                    //        new RDirect { Prop = "http://fogid.net/o/inDocument"  }
                    //    }  } }, 
                    new RInverse { Prop = "http://fogid.net/o/in-doc", IRec = new RRecord { Props = new RProperty[]
                        {
                            new RField { Prop = "http://fogid.net/o/ground" },
                            new RDirect { Prop = "http://fogid.net/o/reflected" }
                        }  } },

                }
            };
            RRecord tree = BuildTree(rec.Id, format);
            Id = tree.Id;
            Tp = tree.Tp;
            tp = Infobase.rontology.LabelOfOnto(tree.Tp);
            name = tree.GetName();
            fd = tree.GetField("http://fogid.net/o/from-date");
            td = tree.GetField("http://fogid.net/o/to-date");
            description = tree.GetField("http://fogid.net/o/description");
            doccontent = tree.GetField("http://fogid.net/o/doc-content");
            uri = tree.GetField("http://fogid.net/o/uri");

            List<RRecord> tits = new List<RRecord>();
            List<RRecord> parts = new List<RRecord>();
            List<RRecord> livs = new List<RRecord>();
            List<RRecord> partis = new List<RRecord>();
            List<RRecord> indoc_docpart = new List<RRecord>();
            //List<RRecord> item_docpart = new List<RRecord>();
            List<RRecord> indoc_refl = new List<RRecord>();
            foreach (var qq in tree.Props.Where(p => p is RInverse))
            {
                RInverse inv = (RInverse)qq;
                if (inv.Prop == "http://fogid.net/o/has-title")
                {
                    tits.Add(inv.IRec);
                }
                else if (inv.Prop == "http://fogid.net/o/participant")
                {
                    string resource = inv.IRec.GetDirectResource("http://fogid.net/o/in-org");
                    RRecord r = BuildTree(resource, null);
                    parts.Add(new RRecord
                    {
                        Id = inv.IRec.Id,
                        Tp = inv.IRec.Tp,
                        Props = inv.IRec.Props.Select(p =>
                        {
                        if (p is RLink && p.Prop == "http://fogid.net/o/in-org")
                        return new RDirect { Prop = p.Prop, DRec = r };
                        else return p;
                        }).ToArray()
                    });
                }
                else if (inv.Prop == "http://fogid.net/o/something")
                {
                    //string resource = inv.IRec.GetDirectResource("http://fogid.net/o/location-place");
                    livs.Add(inv.IRec);
                }
                else if (inv.Prop == "http://fogid.net/o/in-org")
                {
                    partis.Add(inv.IRec);
                }
                else if (inv.Prop == "http://fogid.net/o/inDocument")
                {
                    indoc_docpart.Add(inv.IRec);
                }
                else if (inv.Prop == "http://fogid.net/o/partItem")
                {
                    indoc_docpart.Add(inv.IRec);
                }
                else if (inv.Prop == "http://fogid.net/o/in-doc")
                {
                    indoc_refl.Add(inv.IRec);
                }
            }
            titles = tits.ToArray();
            livings = livs.ToArray();
            participants = partis.ToArray();
            indoc_parts = indoc_docpart.OrderBy(dp => dp.GetField("http://fogid.net/o/pageNumbers")).ToArray();
            //item_parts = item_docpart.ToArray();
            indoc_reflections = indoc_refl.ToArray();

            if (parts.Count > 0)
            {
                works = parts.Where(part => part.Props.Any(p =>
                {
                    if (p.Prop == "http://fogid.net/o/in-org")
                    {
                        RRecord rec = ((RDirect)p).DRec;
                        string oc = rec.GetField("http://fogid.net/o/org-classification");
                        if (oc == "organization") return true;
                    }
                    return false;
                })).ToArray();
                notworks = parts.Where(part => part.Props.All(p =>
                {
                    if (p.Prop == "http://fogid.net/o/in-org")
                    {
                        RRecord rec = ((RDirect)p).DRec;
                        string oc = rec.GetField("http://fogid.net/o/org-classification");
                        if (oc == "organization") return false;
                    }
                    return true;
                })).ToArray();
            }


        }


        public static RRecord BuildTree(string id, RRecord template)
        {
            if (id == null) return null;
            if (template == null) return (new RXEngine()).GetRRecord(id);
            bool extended = template.Props.Any(p => p is RInverse);
            RRecord erec = (new RXEngine()).GetRRecord(id, extended);
            List<RProperty> props = new List<RProperty>();
            for (int i=0; i<template.Props.Length; i++)
            {
                RProperty frm = template.Props[i];
                string prop_id = frm.Prop;
                if (frm is RField)
                {
                    RField f = new RField { Prop = prop_id, Value = erec.GetField(prop_id), Lang = null };
                    props.Add(f);
                } 
                else if (frm is RDirect)
                {
                    var lnk = erec.GetDirectResource(prop_id);
                    RDirect dir = new RDirect { Prop = prop_id, DRec = BuildTree(lnk, ((RDirect)frm).DRec) };
                    props.Add(dir);
                }
                else if (frm is RInverse)
                {
                    var inv_set = erec.Props.Where(p => p is RInverseLink && p.Prop == prop_id).ToArray();
                    props.AddRange(inv_set.Cast<RInverseLink>()
                        .Select(inv => 
                        {
                            RRecord irec = null;
                            if (inv.Source != null && ((RInverse)frm).IRec != null) irec = BuildTree(inv.Source, ((RInverse)frm).IRec);
                            else if (inv.Source != null && ((RInverse)frm).IRec == null)
                            {
                                var r = (new RXEngine()).GetRRecord(inv.Source);
                                irec = new RRecord
                                {
                                    Id = r.Id,
                                    Tp = r.Tp,
                                    Props =
                                    r.Props.Where(p => !(p is RLink && p.Prop == prop_id)).ToArray()
                                };

                            }
 
                            return new RInverse
                            {
                                Prop = prop_id,
                                IRec =  irec
                            };
                        }));
                }
            }
            RRecord tree = new RRecord { Id = erec.Id, Tp = erec.Tp, Props = props.ToArray() };
            return tree;
        }
    }
    public class PortraitModel3
    {
        public string name, fd, td, description, uri;
        public string dates { get { return (fd == null ? "" : fd) + (string.IsNullOrEmpty(td) ? "" : "-" + td); } }
        public RRecord[] titles = null;
        public RRecord[] works = null;
        public RRecord[] notworks = null;
        public RRecord[] livings = null;

        private PM3 m3;
        public PortraitModel3(RRecord rec)
        {
            m3 = new PM3(rec);
            name = m3.focusRec.GetName();
            fd = m3.focusRec.GetField("http://fogid.net/o/from-date");
            td = m3.focusRec.GetField("http://fogid.net/o/to-date");
            description = m3.focusRec.GetField("http://fogid.net/o/description");
            foreach (var qq in m3.focusInversePropTypes)
            {
                if (qq.Prop == "http://fogid.net/o/titled")
                {
                    titles = qq.lists.SelectMany(l => l.list).ToArray();
                }
            }
        }
    }

    public class PM3
    { 
        public RRecord focusRec = null;
        public Models.InversePropType[] focusInversePropTypes = null;
        
        public PM3(RRecord rec)
        {
            RRecord erec = (new RDFEngine.RXEngine()).GetRRecord(rec.Id, true);
            var query = erec.Props.Where(p => p is RInverseLink)
                .Cast<RInverseLink>() 
                .Select(ril => new RInverse { Prop = ril.Prop, IRec = (new RXEngine()).GetRRecord(ril.Source) })
                .Cast<RInverse>()
                .GroupBy(d => d.Prop)
                .Select(kd => new Models.InversePropType
                {
                    Prop = kd.Key,
                    lists =
                    kd.GroupBy(d => d.IRec.Tp)
                        .Select(dd =>
                        {
                            var qu = dd.Select(x => x.IRec)
                                .Select(rr => new RRecord { Id = rr.Id, Tp = rr.Tp, Props = Infobase.rontology.ReorderFieldsDirects(rr, "ru") })
                                .ToArray();
                            return new Models.InverseType
                            {
                                Tp = dd.Key,
                                list = qu
                            };
                        }).ToArray()
                }).ToArray();
            focusInversePropTypes = query;
            focusRec = new RRecord { Id = erec.Id, Tp = erec.Tp, Props = Infobase.rontology.ReorderFieldsDirects(erec, "ru") };
        }
    }
    public class PortraitModel2
    {
        public P3Model p3m;
        public string name, fd, td, description, uri;
        public string dates { get { return (fd == null ? "" : fd) + (string.IsNullOrEmpty(td) ? "" : "-" + td); } }
        public RRecord[] titles = null;
        public RRecord[] works = null;
        public RRecord[] notworks = null;
        public RRecord[] livings = null;

        public PortraitModel2(RRecord rec)
        {
            // Вычислим обратные ссылки через расширенную запись
            RRecord erec = (new RDFEngine.RXEngine() { User = null }).GetRRecord(rec.Id, true);
            List<string> tit_ids = new List<string>();
            List<string> part_ids = new List<string>();
            List<string> liv_ids = new List<string>();

            foreach (RProperty rprop in erec.Props)
            {
                if (rprop is RField)
                {
                    RField f = (RField)rprop;
                    if (f.Prop == "http://fogid.net/o/name") name = f.Value; //TODO: предусмотреть множественность
                    else if (f.Prop == "http://fogid.net/o/from-date") fd = f.Value;
                    else if (f.Prop == "http://fogid.net/o/to-date") td = f.Value;
                    else if (f.Prop == "http://fogid.net/o/description") description = f.Value;
                    else if (f.Prop == "http://fogid.net/o/uri") uri = f.Value;
                }
                else if (rprop is RLink)
                {
                }
                else if (rprop is RInverseLink)
                {
                    RInverseLink l = (RInverseLink)rprop;
                    if (l.Prop == "http://fogid.net/o/has-title") tit_ids.Add(l.Source);
                    else if (l.Prop == "http://fogid.net/o/participant") part_ids.Add(l.Source);
                    else if (l.Prop == "http://fogid.net/o/something") liv_ids.Add(l.Source);
                }
                //if (tit_ids.Count > 0) titles = tit_ids.Select(t => )
            }
        }
    }
    public class PortraitModel
    {
        public string name, dates, description;
        public RRecord[] titles = null;
        public RRecord[] works = null;
        public RRecord[] notworks = null;
        public RRecord[] livings = null;
        public PortraitModel(RRecord focusrecord)
        {
            name = focusrecord.GetName();
            dates = focusrecord.GetDates();
            description = focusrecord.GetField("http://fogid.net/o/description");
            // Еще можно (?) про папу и маму, потом про детей (?)
            RRecord erecord = (new RDFEngine.RXEngine() { User = null }).GetRRecord(focusrecord.Id, true);
            // Группируем обратные отношения по обратным ссылкам
            var relations = erecord.Props.Where(p => p is RInverseLink && ((RInverseLink)p).Source != null)
                .Cast<RInverseLink>()
                .GroupBy(d => d.Prop)
                .Select(kd => new { propId = kd.Key, list = kd.ToArray() })
                .ToArray();
            titles = relations.FirstOrDefault(r => r.propId == "http://fogid.net/o/has-title")?.list
                .Select(pr => (new RDFEngine.RXEngine() { User = null }).GetRRecord(pr.Source))
                .ToArray();

            var participations = relations.FirstOrDefault(r => r.propId == "http://fogid.net/o/participant")?.list
                .Select(pr =>
                {
                    var rec = (new RDFEngine.RXEngine()).GetRRecord(pr.Source, true);
                    return new RRecord
                    {
                        Id = rec.Id,
                        Tp = rec.Tp,
                        Props = rec.Props.Select(pro =>
                        {
                            if (pro.Prop == pr.Prop) return null; // запрещенное направление
                            RProperty pres = null;
                            if (pro is RField) pres = pro;
                            else if (pro is RLink) pres = new RDirect
                            {
                                Prop = pro.Prop,
                                DRec = (new RDFEngine.RXEngine()).GetRRecord(((RLink)pro).Resource, false)
                            };
                            return pres;
                        }).Where(r => r != null).ToArray()
                    };
                })
                .ToArray();
            // "http://fogid.net/o/org-classification"
            // "organization"
            if (participations != null)
            {
                works = participations.Where(part => part.Props.Any(p =>
                {
                    if (p.Prop == "http://fogid.net/o/in-org")
                    {
                        RRecord rec = ((RDirect)p).DRec;
                        string oc = rec.GetField("http://fogid.net/o/org-classification");
                        if (oc == "organization") return true;
                    }
                    return false;
                })).ToArray();
                notworks = participations.Where(part => part.Props.All(p =>
                {
                    if (p.Prop == "http://fogid.net/o/in-org")
                    {
                        RRecord rec = ((RDirect)p).DRec;
                        string oc = rec.GetField("http://fogid.net/o/org-classification");
                        if (oc == "organization") return false;
                    }
                    return true;
                })).ToArray();
            }

            livings = relations.FirstOrDefault(r => r.propId == "http://fogid.net/o/something")?.list
.Select(pr =>
{
    var rec = (new RDFEngine.RXEngine()).GetRRecord(pr.Source, false);
    return new RRecord
    {
        Id = rec.Id,
        Tp = rec.Tp,
        Props = rec.Props.Select(pro =>
        {
            if (pro.Prop == pr.Prop) return null; // запрещенное направление
            RProperty pres = null;
            if (pro is RField) pres = pro;
            else if (pro is RLink) pres = new RDirect
            {
                Prop = pro.Prop,
                DRec = (new RDFEngine.RXEngine()).GetRRecord(((RLink)pro).Resource, false)
            };
            return pres;
        }).Where(r => r != null).ToArray()
    };
})
.ToArray();


        }
    }
}
