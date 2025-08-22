using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NbtToolkit;
using OpenTK.Mathematics;
using Render;

namespace SchemAnalyser
{
    public static class MaterialUtils
    {
        public static BlockState GetCopyCatMaterial(TagCompound tag)
        {
            return GetBlockState(tag["material"].AsTagCompound());
        }

        public static BlockState GetBlockState(TagCompound tag)
        {
            var properties = new Dictionary<string, string>();
            if (tag.ContainsKey("Properties"))
            {
                foreach (var prop in tag["Properties"].AsTagCompound())
                {
                    properties[prop.Key] = prop.Value.AsString();
                }
            }
            
            return new BlockState()
            {
                Name = tag["Name"].AsString(),
                Properties = properties
            };
        }
        
        
    }
    public class Program
    {
        public static Mapper mapper = new();
        public static void Main(string[] args)
        {
            //create:copycat_base
            mapper.Put("copycats:multistate_copycat", compound =>
            {
                List<BlockState> materials = [];

                if (compound.ContainsKey("material_data"))
                {
                    foreach (var entry in compound["material_data"].AsTagCompound())
                    {
                        var state = MaterialUtils.GetCopyCatMaterial(entry.Value.AsTagCompound());
                        if (state.Name != "create:copycat_base")
                        {
                            materials.Add(state);
                        }
                    }
                }
                
                return materials;
            });
            
            mapper.Put("copycats:copycat", compound =>
            {
                List<BlockState> materials = [];

                if (compound.ContainsKey("Material"))
                {
                    var state = MaterialUtils.GetBlockState(compound["Material"].AsTagCompound());
                    if (state.Name != "create:copycat_base")
                    {
                        materials.Add(state);
                    }
                }
                
                return materials;
            });
            
            mapper.Put("copycats:copycat_sliding_door", compound =>
            {
                List<BlockState> materials = [];

                if (compound.ContainsKey("Material"))
                {
                    var state = MaterialUtils.GetBlockState(compound["Material"].AsTagCompound());
                    if (state.Name != "create:copycat_base")
                    {
                        materials.Add(state);
                    }
                }
                
                return materials;
            });
            
            mapper.Put("framedblocks:framed_tile", compound =>
            {
                List<BlockState> materials = [];

                foreach (var entry in compound)
                    if (entry.Key.StartsWith("camo"))
                    {
                        var mt = entry.Value.AsTagCompound();
                        if (mt.ContainsKey("state"))
                        {
                            materials.Add(MaterialUtils.GetBlockState(mt["state"].AsTagCompound()));
                        }
                    }
                
                return materials;
            });
            

            var rm = new DevResourceMannager("../../../assets");
            var modelloader = new ModelLoader(rm);
            
            var bytes = File.ReadAllBytes("Rapira 3.04 gray.vschem");

            var stream = new MemoryStream(bytes);
            
            var schematic = Schematic.FromStream(stream);

            SchematicVisitor visitor = new SchematicVisitor();
            
            schematic.Visit(visitor);

            var score = 0;

            foreach (var entry in visitor.Data)
            {
                if (entry.Value is int i2)
                {
                    score += i2;
                }
                else if (entry.Value is Dictionary<string, int> i3)
                {
                    score++;
                    foreach (var kv in i3)
                    {
                        score += kv.Value;
                    }
                }
            }
            
            Console.WriteLine(
                JsonSerializer.Serialize(visitor.Data, new JsonSerializerOptions
                {
                    WriteIndented = true
                })
            );
            Console.WriteLine($"Score: {score}");
            
            //using Game wit = new Game();
            //wit.Load += () =>
            //{
            //    var cube = new Model([
            //        0,1,2,2,3,0,  4,5,6,6,7,4,
            //        0,1,5,5,4,0,  2,3,7,7,6,2,
            //        0,3,7,7,4,0,  1,2,6,6,5,1
            //    ], [
            //        -0.5f,-0.5f,-0.5f,  0.5f,-0.5f,-0.5f,  0.5f,0.5f,-0.5f, -0.5f,0.5f,-0.5f,
            //        -0.5f,-0.5f,0.5f,   0.5f,-0.5f,0.5f,   0.5f,0.5f,0.5f,  -0.5f,0.5f,0.5f
            //    ]);
            //    
            //    var b2 = modelloader.Load(rm.ReadToEndOrThrow(
            //        rm["minecraft:models/cube.json"]));
//
//
            //    //for (int x = -128; x < 128; x++)
            //    //{
            //    //    for (int z = -128; z < 128; z++)
            //    //    {
            //    //        wit.objects.Add(new GObject(new Vector3(x, 0, z), b2, new Vector3(200, 100, 2)));
            //    //    }
            //    //}
            //    
            //    foreach (var grid in schematic.ShipGrids)
            //    {
            //        var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            //        var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            //        
            //        foreach (var blockEntry in grid.Blocks)
            //        {
            //            var pos = new Vector3(blockEntry.x, blockEntry.y, blockEntry.z);
//
            //            min.X = Math.Min(min.X, pos.X);
            //            min.Y = Math.Min(min.Y, pos.Y);
            //            min.Z = Math.Min(min.Z, pos.Z);
//
            //            max.X = Math.Max(max.X, pos.X);
            //            max.Y = Math.Max(max.Y, pos.Y);
            //            max.Z = Math.Max(max.Z, pos.Z);
            //        }
            //        
            //        var center = (min + max) * 0.5f;
            //        
            //        foreach (var blockEntry in grid.Blocks)
            //        {
            //            wit.objects.Add(new GObject(
            //                center - new Vector3(blockEntry.x, blockEntry.y, blockEntry.z), 
            //                b2, 
            //                TextToColor(schematic.BlockPallete[blockEntry.pid].Name)
            //                ));
            //        }
            //    }
            //};
            //wit.Run();
        }
        
        public static Vector3 TextToColor(string text)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(text));
                
                float r = hash[0] / 255f;
                float g = hash[1] / 255f;
                float b = hash[2] / 255f;

                return new Vector3(r, g, b);
            }
        }

        private class SchematicVisitor : ISchematicVisitor, IShipVisitor
        {
            private Schematic schematic;
            public Dictionary<string, object> Data = new();
            public void Visit(Schematic schematic)
            {
               this.schematic = schematic;
            }

            public void VisitShipGrid(ShipGrid ships)
            {
                Visit(ships);
                foreach (var block in ships.Blocks)
                {
                    VisitBlock(block);
                }
            }

            public void Visit(ShipGrid ship)
            {
                Console.WriteLine(ship.Id);
            }
            
            private void Increment(string key)
            {
                if (!Data.TryGetValue(key, out var value))
                {
                    Data[key] = 1;
                    return;
                }

                if (value is int i)
                {
                    Data[key] = i + 1;
                }
            }

            private void IncrementNested(string key, IEnumerable<BlockState> states)
            {
                if (!Data.TryGetValue(key, out var value) || value is not Dictionary<string,int> dict)
                {
                    dict = new Dictionary<string,int>();
                    Data[key] = dict;
                }

                foreach (var s in states)
                {
                    dict[s.Name] = dict.TryGetValue(s.Name, out var count) ? count + 1 : 1;
                }
            }

            private void VisitBlock(ShipGrid.BlockEntry entry, BlockState block, TagCompound compound)
            {
                var states = mapper.Map<List<BlockState>>(compound);
                if (states is null)
                {
                    Console.WriteLine(block.Name);
                    return;
                }

                IncrementNested(block.Name, states);
            }

            public void VisitEntity(EntityItem entityItem)
            {
                if (!entityItem.Tag.ContainsKey("Contraption"))
                    return;

                var compound = entityItem.Tag["Contraption"].AsTagCompound()["Blocks"].AsTagCompound();
                List<BlockState> materials = [];

                foreach (var entry in compound["Palette"].AsTagList<TagCompound>())
                    materials.Add(MaterialUtils.GetBlockState(entry));

                foreach (var entry in compound["BlockList"].AsTagList<TagCompound>())
                {
                    var state = materials[entry["State"].AsInt()];
                    if (entry.ContainsKey("Data"))
                    {
                        VisitBlock(null, state, entry["Data"].AsTagCompound());
                        return;
                    }

                    Increment(state.Name);
                }
            }

            public void VisitBlock(ShipGrid.BlockEntry entry)
            {
                string id = schematic.BlockPallete[entry.pid].Name;

                if (entry.edi > -1)
                {
                    var root = schematic.ExtraBlockData[entry.edi];
                    if (root.ContainsKey("id"))
                    {
                        var id_ = root["id"].AsString();
                        if (id_.StartsWith("copycats") || id_.StartsWith("framedblocks"))
                        {
                            VisitBlock(entry, schematic.BlockPallete[entry.pid], root);
                            return;
                        }
                    }
                }

                Increment(id);
            }
        }
    }
}