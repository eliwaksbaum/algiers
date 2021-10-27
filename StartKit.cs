using System;

namespace Algiers.StartKit
{
    public class CMD
    {

        public static Func<string> Look(Player player)
        {
            return () => {
                string output = player.current_room.description;
                foreach (GameObject gameObject in player.current_room.GameObjects)
                {
                    Func<string> look = gameObject.GetTransitiveResponse("look");
                    if (look != null)
                    {
                        output = output + " " + look();
                    }
                }
                return output;
            };
        }

        public static Func<string> Help(World world)
        {
            return () => {
                return world.instructions;
            };
        }

        public static Func<string> Inv(Player player)
        {
            return () => {
                if (player.Inventory.Count == 0)
                {
                    return "Your inventory is empty.";
                }
                else
                {
                    string output = "";
                    foreach (GameObject item in player.Inventory)
                    {
                        string first = item.ID.Substring(0, 1).ToUpper();
                        string rest = item.ID.Substring(1);
                        output = output + first + rest + ", ";
                    }
                    return output.Remove(output.Length - 2);
                }
            };
        }

        public static Func<string, string> What(Player player)
        {
            return (target) => {
                if (!player.CanAccessObject(target))
                {
                    return "There is no " + target + " to examine here.";
                }
                else
                {
                    GameObject targetObj = player.GetObject(target);
                    Func<string> what = targetObj.GetTransitiveResponse("what");
                    if (what == null)
                    {
                        return "You can't examine the " + target + ".";
                    }
                    else
                    {
                        return what();
                    }
                }
            };
        }

        public static Func<string, string> Who(Player player)
        {
            return (person) => {
                if (!player.CanAccessObject(person))
                {
                    return "There is nobody named " + person + " to examine here.";
                }
                else
                {
                    GameObject personObj = player.GetObject(person);
                    Func<string> who = personObj.GetTransitiveResponse(person);
                    if (who == null)
                    {
                        return person + " is not a person.";
                    }
                    else
                    {
                        return who();
                    }
                }
            };
        }

        public static Func<string, string> Take(Player player)
        {
            return (target) => {
                if (player.InInventory(target))
                {
                    return "You already have the " + target + " in your inventory.";
                }
                else if (!player.InRoom(target))
                {
                    return "There is no " + target + " to take here.";
                }
                else
                {
                    GameObject targetObj = player.GetObject(target);
                    Func<string> take = targetObj.GetTransitiveResponse("take");
                    if (take == null)
                    {
                        return "You can't take the " + target + ".";
                    }
                    else
                    {
                        return take();
                    }
                }
            };
        }

        public static Func<string, string> Talk(Player player)
        {
            return (name) => {
                if (!player.InRoom(name))
                {
                    return "There is nobody named " + name + " to talk to here.";
                }
                else
                {
                    GameObject person = player.GetObject(name);
                    Func<string> talk = person.GetTransitiveResponse("talk");
                    if (talk == null)
                    {
                        return "You can't talk to the " + person + ".";
                    }
                    else
                    {
                        return talk();
                    }
                }
            };
        }

        public static Func<string, string> Go(Player player, World world)
        {
            return (newRoom) => {
                string newRoomID = player.current_room.GetExit(newRoom);
                if (newRoomID == null)
                {
                    return "There's no " + newRoom + " to go to from here.";
                }
                else
                {
                    player.current_room.OnExit();
                    player.current_room = world.GetRoom(newRoomID);
                    player.current_room.OnEnter();
                    return world.GetIntransitiveResponse(world.GetCommand("look"))();
                }
            };
        }

        public static Func<string, string, string> Use(Player player)
        {
            return (tool, target) => {
                if (!player.InInventory(tool))
                {
                    string indef = (Parser.StartsWithVowel(tool))? "an " : "a ";
                    return "You don't have " + indef + tool + " in your inventory.";
                }
                
                GameObject toolObj = player.GetObject("tool");
                Func<string> transitiveUse = toolObj.GetTransitiveResponse("use");
                if (transitiveUse != null)
                {
                    return transitiveUse();
                }
                else if (target == "")
                {
                    return "Use " + tool + " on what?";
                }
                else if (!player.CanAccessObject(target))
                {
                    return "There is no " + target + " to use " + tool + " on here.";
                }
                else
                {
                    GameObject targetObj = player.GetObject(target);;
                    Func<string, string> ditransitiveUse = targetObj.GetDitransitiveResponse("use");
                    string nullHandler = "You can't use the " + tool + " with the " + target + ".";
                    if (ditransitiveUse == null)
                    {
                        return nullHandler;
                    }
                    else
                    {
                        string response = ditransitiveUse(tool);
                        return (response == null)? nullHandler : response;
                    }
                }
            };
        }

        public static Func<string, string, string> Give(Player player)
        {
            return (gift, person) => {
                if (!player.InInventory(gift))
                {
                    string indef = (Parser.StartsWithVowel(gift))? "an " : "a ";
                    return "You don't have " + indef + gift + " in you inventory.";
                }
                else if (person == "")
                {
                    return "Give " + gift + " to whom?";
                }
                else if (!player.CanAccessObject(person))
                {
                    return "There is nobody named " + person + " here to give the " + gift + " to.";
                }
                else
                {
                    GameObject personObj = player.GetObject(person);
                    Func<string, string> give = personObj.GetDitransitiveResponse("give");
                    string nullHandler = "You can't give the " + gift + " to the " + person + ".";
                    if (give == null)
                    {
                        return nullHandler;
                    }
                    else
                    {
                        string response = give(gift);
                        return (response == null)? nullHandler : response;
                    }
                }
            };
        }
    }
}