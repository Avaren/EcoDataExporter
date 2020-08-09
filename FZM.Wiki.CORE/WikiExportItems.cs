﻿using Eco.Gameplay.Components;
using Eco.Gameplay.Housing;
using Eco.Gameplay.Items;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Players;
using Eco.Gameplay.Property;
using Eco.Gameplay.Systems.Chat;
using Eco.Shared.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Eco.Gameplay.Pipes.LiquidComponents;
using Eco.Shared.Utils;
using Eco.Shared.Localization;
using Eco.Gameplay;
using Eco.Mods.TechTree;
using Eco.Core.IoC;
using Eco.World;
using System.Text;
using Eco.Gameplay.Skills;

/*
 * This script is an extension by FZM based on the work done by Pradoxzon.
 * 
 * Most code was re-written to make use of changed or new additions to the Eco source code
 * and to change the reliance on Pradoxzon Core Utilities mod.
 *  
 */

namespace FZM.Wiki
{
    public partial class WikiDetails : IChatCommandHandler
    {
        // required for clearing space for objects
        static Vector3i cellSize = new Vector3i(10, 10, 10);

        // dictionary of items and their dictionary of stats
        private static SortedDictionary<string, Dictionary<string, string>> EveryItem = new SortedDictionary<string, Dictionary<string, string>>();

        [ChatCommand("Creates a dump file of all discovered items", ChatAuthorizationLevel.Admin)]
        public static void ItemDetails(User user)
        {
            // dictionary of item properties
            Dictionary<string, string> itemDetails = new Dictionary<string, string>()
            {
                { "category", "nil" },
                { "group", "nil" },
                { "description", "nil" },
                { "tagGroups", "nil"},
                { "maxStack", "nil" },
                { "carried", "nil" },
                { "weight", "nil" },
                { "calories", "nil" },
                { "carbs", "nil" },
                { "protein", "nil" },
                { "fat", "nil" },
                { "vitamins", "nil" },
                { "density", "nil" },
                { "fuel", "nil" },
                { "yield", "nil" },
                { "currency", "nil" },
                { "skillValue", "nil" },
                { "roomCategory", "nil" },
                { "furnitureType", "nil" },
                { "repeatsDepreciation", "nil" },
                { "materialTier", "nil" },
                { "fuelsUsed", "nil" },
                { "gridRadius", "nil" },
                { "energyUsed", "nil" },
                { "energyProduced", "nil" },
                { "energyType", "nil" },
                { "fluidsUsed", "nil"},
                { "fluidsProduced" , "nil"},
                { "validTalents", "nil" },
                { "footprint", "nil" },
                { "mobile", "nil" },
                { "roomSizeReq", "nil" },
                { "roomMatReq", "nil" },
                { "roomContainReq", "nil" },
                { "inventorySlots", "nil" },
                { "inventoryMaxWeight", "nil" },
                { "type", "nil" },
                { "typeID", "nil" }
            };

            PrepGround(user, (Vector3i)user.Player.Position + new Vector3i(12, 0, 12));
            PrepGround(user, (Vector3i)user.Player.Position + new Vector3i(-12, 0, -12));

            foreach (Item allItem in Item.AllItems)
            {
                //Console.WriteLine("Item: " + allItem.DisplayName);
                if (!EveryItem.ContainsKey(allItem.DisplayName) && (allItem.DisplayName != "Chat Log") && (allItem.DisplayName != "Vehicle Tool Toggle") && (allItem.Group != "Skills") && (allItem.Group != "Talents") && allItem.Group != "Actionbar Items")
                {
                    string displayName = allItem.DisplayName;
                    EveryItem.Add(displayName, new Dictionary<string, string>(itemDetails));
                    EveryItem[displayName]["category"] = "'" + allItem.Category + "'";
                    EveryItem[displayName]["group"] = "'" + allItem.Group + "'";
                    EveryItem[displayName]["type"] = "'" + allItem.Type.ToString().Substring(allItem.Type.ToString().LastIndexOf('.') + 1) + "'";
                    EveryItem[displayName]["typeID"] = "'" + allItem.TypeID.ToString() + "'";

                    Regex regex = new Regex("\t\n\v\f\r");
                    EveryItem[displayName]["description"] = "'" + regex.Replace(CleanTags(allItem.DisplayDescription), " ").Replace("'", "\\'") + "'";

                    StringBuilder tags = new StringBuilder();
                    tags.Append("{");
                    foreach (Tag tag in allItem.Tags())
                    {
                        tags.Append("'[[" + SplitName(tag.DisplayName) + "]]'");

                        if (tag != allItem.Tags().Last())
                            tags.Append(", ");                       
                    }
                    tags.Append("}");
                    EveryItem[displayName]["tagGroups"] = tags.ToString();

                    EveryItem[displayName]["maxStack"] = "'" + allItem.MaxStackSize.ToString() + "'";
                    EveryItem[displayName]["carried"] = allItem.IsCarried ? "'Hands'" : "'Backpack'";
                    EveryItem[displayName]["currency"] = allItem.CanBeCurrency ? "'Yes'" : "nil";
                    if (allItem.HasWeight) { EveryItem[displayName]["weight"] = "'" + ((Decimal)allItem.Weight / 1000).ToString() + "'"; }
                    if (allItem.IsFuel) { EveryItem[displayName]["fuel"] = "'" + allItem.Fuel.ToString() + "'"; }
                    if (allItem.HasYield) { EveryItem[displayName]["yield"] = "'[[" + allItem.Yield.Skill.DisplayName + "]]'"; }

                    #region Food Items

                    // if the item is also a food item get the nutrient values
                    if (allItem is FoodItem)
                    {
                        FoodItem foodItem = allItem as FoodItem;
                        EveryItem[displayName]["calories"] = "'" + foodItem.Calories.ToString("F1") + "'";
                        EveryItem[displayName]["carbs"] = "'" + foodItem.Nutrition.Carbs.ToString("F1") + "'";
                        EveryItem[displayName]["protein"] = "'" + foodItem.Nutrition.Protein.ToString("F1") + "'";
                        EveryItem[displayName]["fat"] = "'" + foodItem.Nutrition.Fat.ToString("F1") + "'";
                        EveryItem[displayName]["vitamins"] = "'" + foodItem.Nutrition.Vitamins.ToString("F1") + "'";
                        if (float.IsNaN(foodItem.Nutrition.Values.Sum() / foodItem.Calories))
                            EveryItem[displayName]["density"] = "'0.0'";
                        else
                            EveryItem[displayName]["density"] = "'" + ((foodItem.Nutrition.Values.Sum() / foodItem.Calories) * 100).ToString("F1") + "'";
                    }

                    #endregion

                    #region Housing Values

                    // if the item is a world item that has a housing category, housing value details still sit on the item so needs to be seperated from Object properties
                    if (allItem.Group == "World Object Items" || allItem.Group == "Modules") //&& allItem.Type != typeof(GasGeneratorItem)?
                    {
                        PropertyInfo[] props = allItem.Type.GetProperties();
                        foreach (var prop in props)
                        {
                            //if (prop.GetValue(allItem) != null) { Console.WriteLine("ItemProperties - " + prop.Name + ": " + prop.GetValue(allItem).ToString()); }
                            if (prop.Name == "HousingVal")
                            {
                                HousingValue v = prop.GetValue(allItem) as HousingValue;
                                EveryItem[displayName]["skillValue"] = "'" + v.Val.ToString() + "'";
                                EveryItem[displayName]["roomCategory"] = "'" + v.Category.ToString() + "'";
                                if (v.Category.ToString() != "Industrial")
                                {
                                    EveryItem[displayName]["furnitureType"] = "'" + v.TypeForRoomLimit.ToString() + "'";
                                    EveryItem[displayName]["repeatsDepreciation"] = "'" + v.DiminishingReturnPercent.ToString() + "'";
                                }
                            }
                        }
                    }

                    #endregion

                    #region Materials & Tiers

                    // if the item is a block then add it's tier
                    if (allItem.Group == "Block Items")
                    {
                        PropertyInfo[] props = allItem.Type.GetProperties();
                        foreach (var prop in props)
                        {
                            if (prop.Name == "Tier")
                            {
                                int t = (int)prop.GetValue(allItem);
                                EveryItem[displayName]["materialTier"] = "'" + t.ToString() + "'";
                            }
                        }
                    }
                    #endregion

                    #region World Objects

                    // for world objects we need to get the object placed in world to access it's properties, each object is destroyed at the end of it's read.
                    if (allItem.Group == "World Object Items" || allItem.Group == "Road Items" || allItem.Group == "Modules") //&& allItem.Type != typeof(GasGeneratorItem)
                    {
                        //Log.WriteLine(Localizer.DoStr(allItem.DisplayName));
                        WorldObjectItem i = allItem as WorldObjectItem;
                        var obj = WorldObjectManager.ForceAdd(i.WorldObjectType, user, (Vector3i)user.Player.Position + new Vector3i(12, 0, 12), Quaternion.Identity);
                        
                        // Couldn't Place the obj
                        if (obj == null)
                        {
                            // Attempt a special placement
                            obj = SpecialPlacement(user, i.WorldObjectType);

                            // Still couldn't place the obj
                            if (obj == null)
                            {
                                Log.WriteLine(Localizer.DoStr("Unable to create instance of " + i.WorldObjectType.Name));
                                continue;
                            }
                        }

                        //PropertyInfo[] props = obj.GetType().GetProperties();

                        EveryItem[displayName]["mobile"] = obj.Mobile ? "'Yes'" : "nil";

                        #region World Object Liquid Components

                        // Checks the objectfor the three liquid components and returns the private fields of those components to the dictionary.
                        // first create a list item and rate strings to attach
                        List<string> consumedFluids = new List<string>();
                        List<string> producedFluids = new List<string>();

                        // We assume each component will only be on the WorldObject once... dangerous with SLG devs.
                        var lp = obj.GetComponent<LiquidProducerComponent>();
                        if (lp != null)
                        {
                            Type producesType = (Type)GetFieldValue(lp, "producesType");
                            float productionRate = (float)GetFieldValue(lp, "constantProductionRate");

                            producedFluids.Add("{'[[" + SplitName(RemoveItemTag(producesType.Name) + "]]', '" + productionRate + "'}"));
                        }

                        var lc = obj.GetComponent<LiquidConsumerComponent>();
                        if (lc != null)
                        {
                            Type acceptedType = lc.AcceptedType;
                            float consumptionRate = (float)GetFieldValue(lc, "constantConsumptionRate");

                            consumedFluids.Add("{'[[" + SplitName(RemoveItemTag(acceptedType.Name) + "]], '" + consumptionRate + "'}"));
                        }

                        var lconv = obj.GetComponent<LiquidConverterComponent>();
                        if (lconv != null)
                        {
                            LiquidProducerComponent convLP = (LiquidProducerComponent)GetFieldValue(lconv, "producer");
                            LiquidConsumerComponent convLC = (LiquidConsumerComponent)GetFieldValue(lconv, "consumer");

                            Type producesType = (Type)GetFieldValue(convLP, "producesType");
                            float productionRate = (float)GetFieldValue(convLP, "constantProductionRate");

                            producedFluids.Add("{'[[" + SplitName(RemoveItemTag(producesType.Name) + "]]', '" + productionRate + "'}"));

                            Type acceptedType = convLC.AcceptedType;
                            float consumptionRate = (float)GetFieldValue(convLC, "constantConsumptionRate");
                            consumedFluids.Add("{'[[" + SplitName(RemoveItemTag(acceptedType.Name) + "]]', '" + consumptionRate + "'}"));
                        }

                        // combine the strings to add to the dictionary
                        foreach (string str in consumedFluids)
                        {
                            if (str == consumedFluids.First())
                                EveryItem[displayName]["fluidsUsed"] = "{" + str;
                            else
                                EveryItem[displayName]["fluidsUsed"] += str;

                            if (str != consumedFluids.Last())
                                EveryItem[displayName]["fluidsUsed"] += ",";
                            else
                                EveryItem[displayName]["fluidsUsed"] += "}";
                        }

                        foreach (string str in producedFluids)
                        {
                            if (str == producedFluids.First())
                                EveryItem[displayName]["fluidsProduced"] = "{" + str;
                            else
                                EveryItem[displayName]["fluidsProduced"] += str;
                            if (str != producedFluids.Last())
                                EveryItem[displayName]["fluidsProduced"] += ",";
                            else
                                EveryItem[displayName]["fluidsProduced"] += "}";
                        }

                        #endregion

                        #region World Object Fuel Supply

                        if (obj.HasComponent<FuelSupplyComponent>())
                        {
                            var fuelComponent = obj.GetComponent<FuelSupplyComponent>();
                            var fuelTags = GetFieldValue(fuelComponent, "fuelTags") as string[];
                            string fuelsString = "[[";
                            foreach (string t in fuelTags)
                            {
                                fuelsString += t;
                                if (t != fuelTags.Last())
                                    fuelsString += "]], [[";
                            }
                            EveryItem[displayName]["fuelsUsed"] = "'" + fuelsString + "]]'";
                        }
                        #endregion

                        #region World Object Power Grid

                        if (obj.HasComponent<PowerGridComponent>())
                        {
                            var gridComponent = obj.GetComponent<PowerGridComponent>();
                            EveryItem[displayName]["energyProduced"] = "'" + gridComponent.EnergySupply.ToString() + "'";
                            EveryItem[displayName]["energyUsed"] = "'" + gridComponent.EnergyDemand.ToString() + "'";
                            EveryItem[displayName]["energyType"] = "'" + gridComponent.EnergyType.Name.ToString() + "'";
                            EveryItem[displayName]["gridRadius"] = "'" + gridComponent.Radius.ToString() + "'";
                        }
                        #endregion

                        #region World Object Room Requirements


                        if (obj.HasComponent<RoomRequirementsComponent>())
                        {
                            var roomRequirementsComponent = obj.GetComponent<RoomRequirementsComponent>();
                            var requirements = RoomRequirements.Get(obj.GetType());
                            if (requirements != null)
                            {
                                foreach (RoomRequirementAttribute a in requirements.Requirements)
                                {
                                    if (a.GetType() == typeof(RequireRoomMaterialTierAttribute))
                                    {
                                        EveryItem[displayName]["roomMatReq"] = "'Tier " + (a as RequireRoomMaterialTierAttribute).Tier + "'";
                                    }
                                    if (a.GetType() == typeof(RequireRoomVolumeAttribute))
                                    {
                                        EveryItem[displayName]["roomSizeReq"] = "'" + (a as RequireRoomVolumeAttribute).Volume + "'";
                                    }
                                    if (a.GetType() == typeof(RequireRoomContainmentAttribute))
                                    {
                                        EveryItem[displayName]["roomContainReq"] = "'Yes'";
                                    }
                                }
                            }
                        }
                        #endregion

                        #region World Object Storage Components

                        if (obj.HasComponent<PublicStorageComponent>())
                        {
                            var psc = obj.GetComponent<PublicStorageComponent>();
                            EveryItem[displayName]["inventorySlots"] = "'" + psc.Inventory.Stacks.Count().ToString() + "'";

                            foreach (InventoryRestriction res in psc.Inventory.Restrictions)
                            {
                                if (res is WeightRestriction)
                                {
                                    WeightRestriction wres = res as WeightRestriction;
                                    WeightComponent wc = (WeightComponent)GetFieldValue(wres, "weightComponent");
                                    EveryItem[displayName]["inventoryMaxWeight"] = "'" + wc.MaxWeight.ToString() + "'";
                                }
                            }
                        }

                        #endregion

                        #region World Object Occupancy

                        if (!obj.Mobile || obj.DisplayName == "Wooden Elevator") // removes vehicles from getting a footprint as they don't have an occupancy
                        {
                            //Console.WriteLine("          Occupancy:");
                            List<BlockOccupancy> Occ = obj.Occupancy;
                            List<int> xList = new List<int>();
                            List<int> yList = new List<int>();
                            List<int> zList = new List<int>();

                            // add the int values of all the blocks of the object to the lists
                            foreach (BlockOccupancy bo in Occ)
                            {
                                xList.Add(bo.Offset.X);
                                yList.Add(bo.Offset.Y);
                                zList.Add(bo.Offset.Z);
                            }

                            // as position 0 is a block we need to add '1' to the range to see the correct footprint size
                            string footprint = (xList.Max() - xList.Min() + 1).ToString() + " X " + (yList.Max() - yList.Min() + 1).ToString() + " X " + (zList.Max() - zList.Min() + 1).ToString();
                            EveryItem[displayName]["footprint"] = "'" + footprint + "'";
                        }

                        #endregion

                        #region Talents

                        if (obj.HasComponent<CraftingComponent>())
                        {
                            var cc = obj.GetComponent<CraftingComponent>();
                            string talentString = "{";
                            foreach (var talent in Talent.AllTalents.Where(x => x.TalentType == typeof(CraftingTalent) && x.Base))
                            {
                                talentString += "'[[" + SplitName(talent.GetType().Name) + "]]'";
                                if (talent != Talent.AllTalents.Where(x => x.TalentType == typeof(CraftingTalent) && x.Base).Last())
                                    talentString += ", ";
                            }                            
                            talentString += "}";
                            EveryItem[displayName]["validTalents"] = talentString;
                        }
                        #endregion

                        obj.Destroy();
                    }

                    #endregion
                }
            }

            WriteDictionaryToFile(user, "Wiki_Module_ItemData.txt", "items", EveryItem);
        }

        private static WorldObject SpecialPlacement(User user,Type worldObjectType)
        {
            if (worldObjectType == typeof(WoodenElevatorObject))
                    return PlaceWoodenElevator(user);

            if (worldObjectType == typeof(WindmillObject) || worldObjectType == typeof(WaterwheelObject))
                    return PlaceWindmill(user, worldObjectType);

            return null;
        }

        private static WorldObject PlaceWindmill(User user, Type worldObjectType)
        {
            int height = 0;
            while (height < 6)
            {
                World.SetBlock(typeof(Eco.World.Blocks.DirtBlock), (Vector3i)user.Player.Position + new Vector3i(-12, height, -12));
                height++;
            }
            
            return WorldObjectManager.ForceAdd(worldObjectType, user, (Vector3i)user.Player.Position + new Vector3i(-11, 5, -12), Quaternion.Identity); 
        }

        private static WorldObject PlaceWoodenElevator(User user)
        {
            var position = user.Player.Position.XYZi + Vector3i.Up;
            WorldObjectDebugUtil.LevelTerrain(new Vector2i(5, 4), position + new Vector3i(-1, 0, -1), typeof(Eco.World.Blocks.DirtBlock), user.Player);
            WorldObjectDebugUtil.CreateShaft(new Vector2i(1, 2), position + new Vector3i(1, 0, 0), user.Player);
            return WorldObjectManager.ForceAdd(ServiceHolder<IWorldObjectManager>.Obj.GetTypeFromName("WoodenElevatorObject"), user, position, Quaternion.Identity, false);
        }

        //Flatten ground, add a border
        private static void PrepGround(User user, Vector3i position)
        {
            var insideType = BlockManager.FromTypeName("DirtRoadBlock");
            var borderType = BlockManager.FromTypeName("StoneRoadBlock");

            WorldObjectDebugUtil.LevelTerrain(cellSize.XZ, position, insideType, user.Player);
            WorldObjectDebugUtil.LevelTerrain(new Vector2i(0, cellSize.Z), position, borderType, user.Player);
            WorldObjectDebugUtil.LevelTerrain(new Vector2i(cellSize.X, 0), position, borderType, user.Player);
        }
    }
}