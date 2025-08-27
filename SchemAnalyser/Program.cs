
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Discore;
using Discore.Http;
using Discore.WebSocket;
using NbtToolkit;
using OpenTK.Mathematics;

namespace SchemAnalyser
{

    public class BlockScore
    {
        public string Name { get; set; }
        public int Score { get; set; }
    }

    public class RootObject
    {
        public List<BlockScore> BlockScores { get; set; }
    }

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

        public static (int ShipGridsCount, int EntityItemsCount, int TotalShipsBlockCount, int TotalShipsPower) GetTotalShipsInfo(byte[] bytes)
        {
            var schematic = Schematic.FromStream(new MemoryStream(bytes));
            Program.SchematicVisitor visitor = new Program.SchematicVisitor();
            schematic.Visit(visitor);
            string BlockScoresPath =
                "C:\\Users\\user\\RiderProjects\\SchemAnalyserScoreBot\\SchemAnalyser\\BlockScores.json";
            var blockScoresList = JsonSerializer.Deserialize<RootObject>(File.ReadAllText(BlockScoresPath));

            int ShipGridsCount = schematic.ShipGrids.Count;
            int EntityItemsCount = schematic.EntityItems.Count;
            int TotalShipsBlockCount = visitor.Data.Values
                .OfType<int>()
                .Sum();
            int TotalShipsPower = 0;
            
            
            Console.WriteLine(
                JsonSerializer.Serialize(visitor.Data, new JsonSerializerOptions
                {
                    WriteIndented = true
                })
            );
            
            foreach (var visitorData in visitor.Data)
            {
                if (visitorData.Value is Dictionary<string, int> stringIntDict)
                {
                    //Console.WriteLine(visitorData.Value);
                    foreach (var kvp in stringIntDict)
                    {
                        //Console.WriteLine($"{kvp.Key} = {kvp.Value}");
                        TotalShipsBlockCount += kvp.Value;
                        
                        var results = from result in blockScoresList.BlockScores
                            where Regex.Match(kvp.Key, result.Name, RegexOptions.Singleline)
                                .Success
                            select result;
                        foreach (var result in results)
                        {
                            TotalShipsPower += result.Score * kvp.Value;
                        }
                    }
                }
                else if (visitorData.Value is int intValue)
                {
                    var results = from result in blockScoresList.BlockScores
                        where Regex.Match(visitorData.Key, result.Name, RegexOptions.Singleline)
                            .Success
                        select result;
                    foreach (var result in results)
                    {
                        TotalShipsPower += result.Score * intValue;
                    }
                }
            }
            TotalShipsPower += TotalShipsBlockCount;
            return (ShipGridsCount, EntityItemsCount, TotalShipsBlockCount, TotalShipsPower);
        }




        public class Program : IDisposable
        {
            public static Mapper mapper = new();

            public static async Task Main(string[] args)
            {


                //var bytes = File.ReadAllBytes(
                //    "C:\\UltimMC\\mmc-cracked-win32\\UltimMC\\instances\\VESTALIS REBORN REBIRTH 2.0 ULTRAKILL\\.minecraft\\VMod-Schematics\\222.vschem");
                //var stream = new MemoryStream(bytes);
                //var schematic = Schematic.FromStream(stream);
                //SchematicVisitor visitor = new SchematicVisitor();
                //schematic.Visit(visitor);
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
                
                mapper.Put("create:copycat", compound =>
                {
                    List<BlockState> materials = [];
    
                    if (compound.ContainsKey("Material"))
                    {
                        var state = GetBlockState(compound["Material"].AsTagCompound());
                        if (state.Name != "create:copycat_base")
                        {
                            materials.Add(state);
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
                
                string token =
                    (await File.ReadAllTextAsync(
                        "C:\\Users\\user\\RiderProjects\\SchemAnalyserScoreBot\\SchemAnalyser\\TOKEN.txt")).Trim();

                using var program = new Program(token);
                await program.Run();
            }




            readonly DiscordHttpClient http;
            readonly Shard shard;
            public Program(string token)
            {
                http = new DiscordHttpClient(token);
                shard = new Shard(token, 0, 1);
            }
            
            public async Task Run()
            {
                await shard.StartAsync(GatewayIntent.GuildMessages | GatewayIntent.MessageContent | GatewayIntent.DirectMessages);
                shard.Gateway.OnMessageCreate += Gateway_OnMessageCreate;
                Console.WriteLine("BOT STARTED");
                await shard.WaitUntilStoppedAsync();
            }

            async void Gateway_OnMessageCreate(object? sender, MessageCreateEventArgs e)
            {
                DiscordMessage message = e.Message;

                if (message.Author.IsBot)
                    return;

                if (message.Content == "!rate" && message.Attachments.Count > 0)
                {
                    try
                    {
                        using (var client = new HttpClient())
                        {
                            byte[] fileData = await client.GetByteArrayAsync(message.Attachments.First().Url);
                            var info = GetTotalShipsInfo(fileData);

                            string response = $"<@!{message.Author.Id}> SCHEMATIC INFO:\n" +
                                              $"Total block count: {info.TotalShipsBlockCount}\n" +
                                              $"Total ship count: {info.ShipGridsCount}\n" +
                                              $"Total entity count: {info.EntityItemsCount}\n\n" +
                                              $"Total ships power: {info.TotalShipsPower}\n";
                            
                            
                                              

                            await http!.CreateMessage(message.ChannelId, response);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        await http!.CreateMessage(message.ChannelId,
                            $"<@!{message.Author.Id}> Error");
                    }
                }
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

            public class SchematicVisitor : ISchematicVisitor, IShipVisitor
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
                    Console.WriteLine($"Ship id: {ship.Id}");
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
                    if (!Data.TryGetValue(key, out var value) || value is not Dictionary<string, int> dict)
                    {
                        dict = new Dictionary<string, int>();
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
                        return;

                    IncrementNested(block.Name, states);
                }

                public void VisitEntity(EntityItem entityItem)
                {
                    if (!entityItem.Tag.ContainsKey("Contraption"))
                        return;

                    var compound = entityItem.Tag["Contraption"].AsTagCompound()["Blocks"].AsTagCompound();
                    List<BlockState> materials = [];

                    foreach (var entry in compound["Palette"].AsTagList<TagCompound>())
                        materials.Add(GetBlockState(entry));

                    foreach (var entry in compound["BlockList"].AsTagList<TagCompound>())
                    {
                        var state = materials[entry["State"].AsInt()];
                        if (entry.ContainsKey("Data"))
                        {
                            VisitBlock(null, state, entry["Data"].AsTagCompound());
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
                            //Console.WriteLine("id: " + id_);
                            if (id_.StartsWith("copycats") || id_.StartsWith("framedblocks") || id_.StartsWith("create:copycat"))
                            {
                                VisitBlock(entry, schematic.BlockPallete[entry.pid], root);
                            }
                        }
                    }

                    Increment(id);
                }
            }

            public void Dispose()
            {
                // Clean up
                http.Dispose();
                shard.Dispose();
            }
        }
    }
}