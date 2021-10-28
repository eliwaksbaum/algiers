using System;
using System.Collections.Generic;

namespace Algiers
{
    //////////
    //ELEMENTS
    //////////

    //WORLD
    public class World
    {
        public bool done = false;
        public string start;
        public string instructions;
        public Player player = new Player();
        public string inputChar = ">";

        Parser.Mode mode = Parser.Mode.Standard;
        public Parser.Mode Mode => mode;
        public void GoStandard()
        {
            mode = Parser.Mode.Standard;
        }
        public void GoRaw(Func<string, string> response)
        {
            mode = Parser.Mode.Raw;
            rawResponse = response;
        }

        Dictionary<string, Room> rooms = new Dictionary<string, Room>();
        public Room GetRoom(string id)
        {
            return rooms[id];
        }

        List<Command> commands = new List<Command>();
        public List<Command> Commands => new List<Command>(commands);

        Dictionary<Command, Func<string>> responses = new Dictionary<Command, Func<string>>();
        Dictionary<Command, Func<string, string>> responsesT = new Dictionary<Command, Func<string, string>>();
        Dictionary<Command, Func<string, string, string>> responsesD = new Dictionary<Command, Func<string, string, string>>();
        Func<string, string> rawResponse;
        public Func<string, string> RawResponse => rawResponse;

        void CheckCommand(Command newcmd)
        {
            foreach (Command cmd in Commands)
            {
                if (newcmd.Equals(cmd))
                {
                    throw new Exception("A Command with phrase \"" + cmd.Phrase + "\" and overlapping validity already exists.");
                }
            }
        }

        public Command AddIntransitiveCommand(string id, Func<string> response, State state, string[] aliases = null, string[] preps = null)
        {
            Command cmd = new Command(CommandType.Intransitive, id, state, _aliases: aliases, _preps: preps);
            CheckCommand(cmd);
            commands.Add(cmd);
            responses.Add(cmd, response);
            return cmd;
        }
        public Command AddTransitiveCommand(string id, Func<string, string> responseT, State state, string missingTargetError, string[] aliases = null, string[] preps = null)
        {
            Command cmd = new Command(CommandType.Transitive, id, state, missingTargetError, _aliases: aliases, _preps: preps);
            CheckCommand(cmd);
            commands.Add(cmd);
            responsesT.Add(cmd, responseT);
            return cmd;
        }
        public Command AddDitransitiveCommand(string id, Func<string, string, string> responseD, State state, string missingTargetError, string[] dipreps, string[] aliases = null)
        {
            Command cmd = new Command(CommandType.Ditransitive, id, state, missingTargetError, dipreps, aliases);
            CheckCommand(cmd);
            commands.Add(cmd);
            responsesD.Add(cmd, responseD);
            return cmd;
        }

        public void SetIntransitiveCommand(Command cmd, Func<string> response)
        {
            responses[cmd] = response;
        }
        public void SetTransitiveCommand(Command cmd, Func<string, string> responseT)
        {
            responsesT[cmd] = responseT;
        }
        public void SetDitransitiveCommand(Command cmd, Func<string, string, string> responseD)
        {
            responsesD[cmd] = responseD;
        }

        public Command GetCommand(string phrase)
        {
            foreach(Command cmd in commands)
            {
                if (player.state.IsWithin(cmd.Validity))
                {
                    if (phrase == cmd.Phrase)
                    {
                        return cmd;
                    }
                    else if (cmd.Aliases != null)
                    {
                        foreach (string nickname in cmd.Aliases)
                        {
                            if (phrase == nickname)
                            {
                                return cmd;
                            }
                        }
                    }
                }
            }
            return null;
        }

        public Func<string> GetIntransitiveResponse(Command cmd)
        {
            return responses[cmd];
        }
        public Func<string, string> GetTransitiveResponse(Command cmd)
        {
            return responsesT[cmd];
        }
        public Func<string, string, string> GetDitransitiveResponse(Command cmd)
        {
            return responsesD[cmd];
        }
        
        public Room AddRoom(string roomID)
        {
            Room newRoom = new Room(roomID);
            rooms.Add(roomID, newRoom);
            return newRoom;
        }
    }

    public class State
    {
        static int counter = 1;
        public static State All = new State(~0);

        int value;
        public int code => value;

        public State()
        {
            value = counter;
            counter <<= 1;
        }

        State(int _value)
        {
            value = _value;
        }

        public State Compose(State other)
        {
            return new State(value | other.value);
        }

        public State Inverse()
        {
            return new State(~value);
        }

        public bool IsWithin(int validity)
        {
            return (validity & value) == value;
        }
    }

    //PLAYER
    public class Player
    {
        public State state;
        public Room current_room;

        Dictionary<string, GameObject> inventory = new Dictionary<string, GameObject>();
        public Dictionary<string, GameObject>.ValueCollection Inventory => inventory.Values;

        List<string> waypoints = new List<string>();
        public List<string> Waypoints => waypoints;

        Dictionary<string, int> counters = new Dictionary<string, int>();

        public void AddWaypoint(string newPoint)
        {
            if (!waypoints.Contains(newPoint))
            {
                waypoints.Add(newPoint);
            }
        }

        public void AddCounter(string counter)
        {
            counters.Add(counter, 0);
        }
        public int GetCounter(string counter)
        {
            return counters[counter];
        }
        public void IncrementCounter(string counter, int increment = 1)
        {
            if (counters.ContainsKey(counter))
            {
                counters[counter] += increment;
            }
        }

        public bool InInventory(GameObject target)
        {
            return inventory.ContainsValue(target);
        }
        public bool InInventory (string target)
        {
            return inventory.ContainsKey(target);
        }

        public bool InRoom(string target)
        {
            return current_room.InRoom(target);
        }

        public bool CanAccessObject(string target)
        {
            return InInventory(target) || InRoom(target);
        }

        public GameObject GetObject(string target)
        {
            if (InInventory(target))
            {
                return inventory[target];
            }

            if (InRoom(target))
            {
                return current_room.GetObject(target);
            }

            return null;
        }

        public void AddToInventory(GameObject target)
        {
            inventory.Add(target.ID, target);
        }

        public void AddToInventoryFrom(GameObject target, IOrigin origin)
        {
            if (origin.Contains(target))
            {
                inventory.Add(target.ID, target);
                origin.RemoveObject(target);
            }
        }
        public void RemoveFromInventory(GameObject target)
        {
            inventory.Remove(target.ID);
        }
    }

    //ORIGIN
    public interface IOrigin
    {
        void RemoveObject(GameObject target);
        bool Contains(string objID);
        bool Contains(GameObject obj);
    }

    //ROOM
    public class Room : IOrigin
    {
        public Room(string _id)
        {
            id = _id;
        }

        string id;
        public string ID => id;
        public string description;
        public Action OnEnter = () => {};
        public Action OnExit = () => {};

        Dictionary<string, string> exits = new Dictionary<string, string>();
        public string GetExit(string exit)
        {
            return exits.ContainsKey(exit) ? exits[exit] : null;
        }

        Dictionary<string, GameObject> gameObjects = new Dictionary<string, GameObject>();
        public bool Contains(string objID)
        {
            return gameObjects.ContainsKey(objID);
        }
        public bool Contains(GameObject obj)
        {
            return gameObjects.ContainsValue(obj);
        }
        public Dictionary<string, GameObject>.ValueCollection GameObjects => gameObjects.Values;

        List<Container> containers = new List<Container>();

        public T AddObject<T>(string objID) where T : GameObject
        {
            GameObject newObj;

            if (typeof(T) == typeof(Container))
            {
                newObj = new Container(objID);
                containers.Add((Container) newObj);
            }
            else
            {
                Object[] args = new Object[] {objID};
                newObj = (T) Activator.CreateInstance(typeof(T), args);
            }

            gameObjects.Add(objID, newObj);
            return (T) newObj;
        }

        public void RemoveObject(GameObject item)
        {
            if (gameObjects.ContainsValue(item))
            {
                gameObjects.Remove(item.ID);
            }
        }

        public void AddExit(string goWord, string roomID)
        {
            exits.Add(goWord, roomID);
        }

        public bool InRoom(string target)
        {
            bool inBase = Contains(target);
            foreach (Container container in containers)
            {
                if (container.Contains(target))
                {
                    return true;
                }
            }
            return inBase;
        }

        public GameObject GetObject(string target)
        {
            if (Contains(target))
            {
                return gameObjects[target];
            }
            else
            {
                foreach (Container container in containers)
                {
                    if (container.Contains(target))
                    {
                        return container.GetObject(target);
                    }
                }
            }
            return null;
        }
    }

    //GAMEOBJECTS
    public class GameObject
    {
        public GameObject(string _id)
        {
            id = _id;
        }

        string id;
        public string ID => id;
        Dictionary<string, Func<string>> responsesT = new Dictionary<string, Func<string>>();
        Dictionary<string, Func<string, string>> responsesD = new Dictionary<string, Func<string, string>>();
        Dictionary<string, bool> conditions = new Dictionary<string, bool> ();

        public Func<string> GetTransitiveResponse(string id)
        {
            return responsesT.ContainsKey(id) ? responsesT[id] : null;
        }
        public Func<string, string> GetDitransitiveResponse(string id)
        {
            return responsesD.ContainsKey(id) ? responsesD[id] : null;
        }

        public void SetTransitiveCommand(string id, Func<string> responseT)
        {
            responsesT.Add(id, responseT);
        }
        public void SetDitransitiveCommand(string id, Func<string, string> responseD)
        {
            responsesD.Add(id, responseD);
        }

        public void SetCondition(string condition, bool value)
        {
            if (conditions.ContainsKey(condition))
            {
                conditions[condition] = value;
            }
            else
            {
                conditions.Add(condition, value);
            }
        }
        public bool GetCondition(string condition)
        {
            return conditions[condition];
        }
    }

    public class Container : GameObject, IOrigin
    {
        public Container(string _id) : base(_id) {}

        Dictionary<string, GameObject> items = new Dictionary<string, GameObject>();
        public bool Contains(string objID)
        {
            return items.ContainsKey(objID);
        }
        public bool Contains(GameObject obj)
        {
            return items.ContainsValue(obj);
        }

        public GameObject AddObject(string itemID)
        {
            GameObject newItem = new GameObject(itemID);
            items.Add(itemID, newItem);

            return newItem;
        }
        public GameObject GetObject(string itemID)
        {
            return items[itemID];
        }
        public void RemoveObject(GameObject item)
        {
            if (Contains(item))
            {
                items.Remove(item.ID);
            }
        }
    }

    public class Person : GameObject
    {
        public Person(string _id) : base(_id) {}

        List<string> gifts = new List<string>();
        public List<string> Gifts => gifts;

        public void AddGift(string giftID)
        {
            gifts.Add(giftID);
        }
    }

    ////////
    //PARSER
    ////////

    public enum CommandType {Intransitive, Transitive, Ditransitive}
    public class Command
    {
        public Command(CommandType _type, string _id, State state,
            string _missingTargetError = null, string[] _dipreps = null, string[] _aliases = null, string[] _preps = null)
        {
            id = _id;
            type = _type;
            validity = state.code;
            missingTargetError = _missingTargetError;
            dipreps = _dipreps;
            aliases = _aliases;
            preps = _preps;
        }
        
        CommandType type;
        public CommandType Type => type;
        string id;
        public string Phrase => id;
        int validity;
        public int Validity => validity;
        string[] preps;
        public string[] Preps => preps;
        string[] dipreps;
        public string[] Dipreps => dipreps;
        string [] aliases;
        public string [] Aliases => aliases;
        string missingTargetError;
        public string MissingTargetError => missingTargetError;

        public bool Overlaps(Command other)
        {
            int intersection = this.validity & other.validity;
            return (this.id == other.id) && (intersection != 0);
        }
    }

    public class Parser
    {
        static public string Clear = Environment.NewLine + Environment.NewLine;
        public enum Mode {Standard, Raw}

        World world;

        public Parser(World _world)
        {
            world = _world;
        }
        
        public string Parse(string input, Mode mode)
        {
            input = input.ToLower();

            if (String.IsNullOrWhiteSpace(input))
            {
                return "Please type a command.";
            }
            switch (mode)
            {
                case Mode.Standard:
                    return StandardParse(input);
                case Mode.Raw:
                    return RawParse(input);
                default:
                    throw new Exception("Unexpected Parser.Mode value.");
            }
        }

        string RawParse(string input)
        {
            return world.RawResponse(input);
        }

        string StandardParse(string input)
        {
            string[] words = (input.Contains(" "))?
                input.Split(" ", StringSplitOptions.RemoveEmptyEntries) : new string[]{input};

            Command cmd = world.GetCommand(words[0]);
            if (cmd == null)
            {
                return "You can't " + words[0] + " right now.";
            }           
            List<string> remainder = GetRemainderList(cmd, words);

            return HandleType(cmd, remainder);
        }

        List<string> GetRemainderList(Command cmd, string[] words)
        {
            List<string> remainder = new List<string>();
            foreach (string word in words)
            {
                remainder.Add(word);
            }

            remainder.RemoveAt(0);
            if (cmd.Preps != null && remainder.Count > 0)
            {
                foreach (string prep in cmd.Preps)
                {
                    if (remainder[0] == prep)
                    {
                        remainder.RemoveAt(0);
                        break;
                    }
                }
            }
            return remainder;
        }

        string HandleType(Command cmd, List<string> remainder)
        {
            switch (cmd.Type)
            {
                case CommandType.Intransitive:
                    return HandleIntransitive(cmd, remainder);
                case CommandType.Transitive:
                    return HandleTransitive(cmd, remainder);
                case CommandType.Ditransitive:
                    return HandleDitransitive(cmd, remainder);
                default:
                    throw new Exception("Unexpected CommandType value.");
            }
        }

        string HandleIntransitive(Command cmd, List<string> remainder)
        {
            if (remainder.Count > 0)
            {
                return cmd.Phrase + " shouldn't have any words after it.";
            }
            else
            {
                return world.GetIntransitiveResponse(cmd)();
            }
        }

        string HandleTransitive(Command cmd, List<string> remainder)
        {
            if (remainder.Count > 1)
            {
                return "Only one word should come after " + cmd.Phrase + ".";
            }
            else if (remainder.Count < 1)
            {
                return cmd.MissingTargetError;
            }
            else
            {
                string objID = remainder[0];
                return world.GetTransitiveResponse(cmd)(objID);
            }
        }

        string HandleDitransitive(Command cmd, List<string> remainder)
        {
            //Make sure we have an object1
            if (remainder.Count < 1)
            {
                return cmd.MissingTargetError;
            }
            else
            {
                //Get obj1
                string obj1ID = remainder[0];
                remainder.RemoveAt(0);

                if (remainder.Count == 0)
                {
                    return world.GetDitransitiveResponse(cmd)(obj1ID, "");
                }

                //Deal with diprep
                bool goodDiprep = false;
                foreach (string diprep in cmd.Dipreps)
                {
                    if (remainder[0] == diprep)
                    {
                        goodDiprep = true;
                        break;
                    }
                }
                if (!goodDiprep)
                {
                    return cmd.Phrase + " .. " + remainder[0] + " is not a valid command. Try " + cmd.Phrase + " .. " + cmd.Dipreps[0] + " instead.";
                }
                else
                {
                    remainder.RemoveAt(0);
                    string obj2ID = (remainder.Count < 1)? "" : remainder[0];
                    return world.GetDitransitiveResponse(cmd)(obj1ID, obj2ID);
                }
            }
        }

        public static bool StartsWithVowel(string str)
        {
            switch (str.Substring(0, 1))
            {
                case "a":
                    return true;
                case "e":
                    return true;
                case "i":
                    return true;
                case "o":
                    return true;
                case "u":
                    return true;
                default:
                    return false;
            }
        }
    }
}