using System;
using System.Collections.Generic;

namespace Algiers
{
    //////////
    //ELEMENTS
    //////////

    public abstract class Element
    {
        protected Element parent;
        public Element Parent => parent;

        protected List<Element> children = new List<Element>();
        public List<Element> Children => children;

        protected virtual void Remove(Element target)
        {
            children.Remove(target);
        }

        public void Delete()
        {
            if (parent != null)
            {
                parent.Remove(this);
            }
        }

        protected void Adopt(Element child)
        {
            if (child.parent != null)
            {
                child.parent.Remove(child);
            }
            child.parent = this;
            children.Add(child);
        }
    }

    //WORLD
    public class World : Element
    {
        protected override void Remove(Element target)
        {
            base.Remove(target);
            rooms.Remove(((Room)target).ID);
        }

        public bool done = false;
        public string start;
        public string instructions;
        static World world;
        public static World GetWorld => world;

        public World()
        {
            world = this;
        }
        
        public Player player = new Player();

        Dictionary<string, Room> rooms = new Dictionary<string, Room>();
        public Room AddRoom(string roomID)
        {
            Room newRoom = new Room(roomID);
            Adopt(newRoom);
            rooms.Add(roomID, newRoom);
            return newRoom;
        }
        public void AddRoom(Room room)
        {
            Adopt(room);
            rooms.Add(room.ID, room);
        }
        public Room GetRoom(string id)
        {
            return rooms[id];
        }

        List<Command> commands = new List<Command>();
        public List<Command> Commands => new List<Command>(commands);

        Dictionary<Command, Func<string>> responses = new Dictionary<Command, Func<string>>();
        Dictionary<Command, Func<string, string>> responsesT = new Dictionary<Command, Func<string, string>>();
        Dictionary<Command, Func<string, string, string>> responsesD = new Dictionary<Command, Func<string, string, string>>();

        void CheckCommand(Command newcmd)
        {
            string newphrase = newcmd.Phrase;
            int newlength = newcmd.Aliases != null ? newcmd.Aliases.Length + 1 : 1;
            string[] newphrases = new string[newlength];
            newphrases[0] = newphrase;
            if (newphrases.Length > 1)
            {
                for (int i = 1; i < newphrases.Length; i++)
                {
                    newphrases[i] = newcmd.Aliases[i-1];
                }
            }

            foreach (Command cmd in Commands)
            {
                if (newcmd.Overlaps(cmd))
                {
                    string phrase = cmd.Phrase;
                    int length = cmd.Aliases != null ? cmd.Aliases.Length + 1 : 1;
                    string[] phrases = new string[length];
                    phrases[0] = phrase;
                    if (phrases.Length > 1)
                    {
                        for (int i = 1; i < phrases.Length; i++)
                        {
                            phrases[i] = cmd.Aliases[i-1];
                        }
                    }

                    foreach(string s in phrases)
                    {
                        foreach(string newS in newphrases)
                        {
                            if (s == newS)
                            {
                                throw new Exception("A Command with the phrase or alias \"" + s + "\" with overlapping validity already exists.");
                            }
                        }
                    }
                }
            }
        }

        public Command AddIntransitiveCommand(string id, Func<string> response, State state, string[] aliases = null)
        {
            Command cmd = new Command(CommandType.Intransitive, id, state, _aliases: aliases);
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

        public void SetIntransitiveResponse(Command cmd, Func<string> response)
        {
            responses[cmd] = response;
        }
        public void SetTransitiveResponse(Command cmd, Func<string, string> responseT)
        {
            responsesT[cmd] = responseT;
        }
        public void SetDitransitiveResponse(Command cmd, Func<string, string, string> responseD)
        {
            responsesD[cmd] = responseD;
        }

        public Command GetCommand(string phrase)
        {
            List<Command> possibles = commands.FindAll((Command cmd) => {
                if (phrase == cmd.Phrase)
                {
                    return true;
                }
                else if (cmd.Aliases != null)
                {
                    foreach (string nickname in cmd.Aliases)
                    {
                        if (phrase == nickname)
                        {
                            return true;
                        }
                    }
                    return false;
                }
                else
                {
                    return false;
                }
            });

            if (possibles.Count > 0)
            {
                Command valid = possibles.Find((Command cmd) => {
                    return player.State.IsWithin(cmd.Validity);
                });
                if (valid != null)
                {
                    return valid;
                }
                else
                {
                    Command ddefault = possibles.Find((Command cmd) => {
                        return cmd.Validity.Code == 0;
                    });
                    if (player.State.Opt_In && ddefault != null)
                    {
                        return ddefault;
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
    }

    //STATE
    public class State
    {
        static int counter = 1;
        public static State All = new State(~0);
        public static State Default = new State(0);

        int value;
        public int Code => value;
        bool isPrime;
        public bool IsPrime => isPrime;
        bool opt_in;
        public bool Opt_In => opt_in;

        public State(bool default_opt_in = true)
        {
            value = counter;
            counter <<= 1;
            isPrime = true;
            opt_in = default_opt_in;
        }

        State(int _value)
        {
            value = _value;
            isPrime = false;
        }

        public State Compose(State other)
        {
            return new State(value | other.value);
        }

        public State Inverse()
        {
            return new State(~value);
        }

        public bool IsWithin(State other)
        {
            return (other.Code & value) == value;
        }
    }

    //PLAYER
    public class Player : Element
    {
        protected override void Remove(Element target)
        {
            DeleteFromInventory((GameObject) target);
        }

        State state;
        public State State
        {
            get => state;
            set
            {
                if (value.IsPrime)
                {
                    state = value;
                }
                else
                {
                    throw new Exception("Player.State can only be assigned prime States (no compositions).");
                }
            }
        }

        public Room current_room;

        Dictionary<string, GameObject> inventory = new Dictionary<string, GameObject>();
        public Dictionary<string, GameObject>.ValueCollection Inventory => inventory.Values;

        List<string> waypoints = new List<string>();

        Dictionary<string, int> counters = new Dictionary<string, int>();

        public void AddWaypoint(string newPoint)
        {
            if (!waypoints.Contains(newPoint))
            {
                waypoints.Add(newPoint);
            }
        }
        public bool HasWaypoint(string point)
        {
            return waypoints.Contains(point);
        }
        
        public int AddCounter(string counter, int init = 0)
        {
            counters.Add(counter, init);
            return init;
        }
        public int GetCounter(string counter)
        {
            return counters[counter];
        }
        public int IncrementCounter(string counter, int increment = 1)
        {
            if (counters.ContainsKey(counter))
            {
                counters[counter] += increment;
            }
            return counters[counter];
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
            GameObject invObj = GetFromInventory(target);
            return invObj != null ? invObj : GetFromRoom(target);
        }
        public GameObject GetFromInventory(string target)
        {
            if (InInventory(target))
            {
                return inventory[target];
            }
            return null;
        }
        public GameObject GetFromRoom(string target)
        {
            if (InRoom(target))
            {
                return current_room.GetObject(target);
            }
            return null;
        }

        public void AddToInventory(GameObject target)
        {
            if (inventory.ContainsKey(target.ID) && target.Stackable)
            {
                GameObject old = GetFromInventory(target.ID);
                if (InventoryStack.IsStack(old))
                {
                    //target.Delete();
                    InventoryStack stack = (InventoryStack) old;
                    target = stack.Increment();
                }
                else
                {
                    target = new InventoryStack(old);
                }
            }
            Adopt(target);
            inventory.Add(target.ID, target);
        }
        public void RemoveFromInventory(GameObject target)
        {
            if (InventoryStack.IsStack(target))
            {
                InventoryStack stack = (InventoryStack) target;
                AddToInventory(stack.Decrement());
            }
            else
            {
                inventory.Remove(target.ID);
                base.Remove(target);
            }
        }
        public void RemoveFromInventory(String target)
        {
            if (inventory.ContainsKey(target))
            {
                GameObject targetObj = inventory[target];
                if (InventoryStack.IsStack(targetObj))
                {
                    InventoryStack stack = (InventoryStack) targetObj;
                    AddToInventory(stack.Decrement());
                }
                else
                {
                    inventory.Remove(target);
                    base.Remove(targetObj);
                }
            }
        }

        void DeleteFromInventory(GameObject target)
        {
            inventory.Remove(target.ID);
            base.Remove(target);
        }
    }

    //ROOM
    public class Room : Element
    {
        public Room(string _id)
        {
            id = _id;
        }
        protected override void Remove(Element target)
        {
            RemoveObject((GameObject) target);
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

        public void AddObject(GameObject item)
        {
            Adopt(item);
            gameObjects.Add(item.ID, item);
        }
        public void RemoveObject(GameObject item)
        {
            gameObjects.Remove(item.ID);
            base.Remove(item);
        }

        public void AddExit(string goWord, string roomID)
        {
            exits.Add(goWord, roomID);
        }

        public bool InRoom(string target)
        {
            bool inBase = Contains(target);
            foreach (GameObject gameobj in GameObjects)
            {
                if (typeof(Container).IsInstanceOfType(gameobj))
                {
                    Container container = (Container) gameobj;
                    if (container.Contains(target))
                    {
                        return true;
                    }
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
                foreach (GameObject gameobj in GameObjects)
                {
                    if (typeof(Container).IsInstanceOfType(gameobj))
                    {
                        Container container = (Container) gameobj;
                        if (container.Contains(target))
                        {
                            return container.GetObject(target);
                        }
                    }
                }
            }
            return null;
        }
    }

    //GAMEOBJECTS
    public class GameObject : Element
    {
        public GameObject(string _id, string name = null, bool stackable = false)
        {
            id = _id;
            this.stackable = stackable;
            this.name = name == null ? id : name;
        }

        string id;
        public string ID => id;
        string name;
        public string Name => name;
        bool stackable;
        public bool Stackable => stackable;
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

        public void SetTransitiveResponse(string id, Func<string> responseT)
        {
            responsesT.Add(id, responseT);
        }
        public void SetDitransitiveResponse(string id, Func<string, string> responseD)
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

        public GameObject Copy()
        {
            GameObject copy = new GameObject(id, name, stackable);

            foreach(KeyValuePair<string, Func<string>> rt in responsesT)
            {
                copy.SetTransitiveResponse(rt.Key, rt.Value);
            }
            foreach(KeyValuePair<string, Func<string, string>> rd in responsesD)
            {
                copy.SetDitransitiveResponse(rd.Key, rd.Value);
            }
            foreach(KeyValuePair<string, bool> con in conditions)
            {
                copy.SetCondition(con.Key, con.Value);
            }

            return copy;
        }
        public void CopyResponsesTo(GameObject mold)
        {
            foreach(KeyValuePair<string, Func<string>> rt in responsesT)
            {
                mold.SetTransitiveResponse(rt.Key, rt.Value);
            }
            foreach(KeyValuePair<string, Func<string, string>> rd in responsesD)
            {
                mold.SetDitransitiveResponse(rd.Key, rd.Value);
            }
            foreach(KeyValuePair<string, bool> con in conditions)
            {
                mold.SetCondition(con.Key, con.Value);
            }
        }
    }

    public abstract class Container : GameObject
    {
        public Container(string _id) : base(_id) {}

        protected override void Remove(Element target)
        {
            RemoveObject((GameObject) target);
        }

        protected Dictionary<string, GameObject> items = new Dictionary<string, GameObject>();
        public bool Contains(string objID)
        {
            return items.ContainsKey(objID);
        }
        public bool Contains(GameObject obj)
        {
            return items.ContainsValue(obj);
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
                base.Remove(item);
            }
        }
    }

    public class Chest : Container
    {
        public Chest(string _id) : base(_id) {}

        public void AddObject(GameObject item)
        {
            Adopt(item);
            items.Add(item.ID, item);
        }
    }

    public class Hoard : Container
    {
        string memberID;
        GameObject exposedMember;

        public Hoard(string _id, string member) : base(_id)
        {
            memberID = member;
            exposedMember = new GameObject(member, stackable: true);
            Adopt(exposedMember);
            items.Add(exposedMember.ID, exposedMember);
        }
        public Hoard(string _id, GameObject origin) : base(_id)
        {
            if (origin.Stackable)
            {
                memberID = origin.ID;
                exposedMember = origin.Copy();
                Adopt(exposedMember);
                items.Add(memberID, exposedMember);
            }
            else
            {
                throw new Exception("The origin of a Hoard must be stackable.");
            }
        }

        public virtual GameObject TakeMember()
        {
            GameObject temp = exposedMember;
            GameObject newMember = exposedMember.Copy();
            Adopt(newMember);
            items[memberID] = newMember;
            exposedMember = newMember;

            base.Remove(temp);
            return temp;
        }

        public void SetMemberTransitiveResponse(string id, Func<string> response)
        {
            exposedMember.SetTransitiveResponse(id, response);
        }
        public void SetMemberDitransitiveResponse(string id, Func<string, string> response)
        {
            exposedMember.SetDitransitiveResponse(id, response);
        }
    }

    public class NumHoard : Hoard
    {
        int count;
        public int Count => count;

        public NumHoard(string _id, string member, int count) : base(_id, member)
        {
            if (count > 0)
            {
                this.count = count;
            }
            else
            {
                throw new Exception("A CountStack must be initialized with a count greater than 0.");
            }
        }
        public NumHoard(string _id, GameObject member, int count) : base(_id, member)
        {
            if (count > 0)
            {
                this.count = count;
            }
            else
            {
                throw new Exception("A CountStack must be initialized with a count greater than 0.");
            }
        }

        public override GameObject TakeMember()
        {
            GameObject member = base.TakeMember();
            count -= 1;

            if (count == 0)
            {
                Delete();
            }
            return member;
        }
    }

    public class InventoryStack : GameObject
    {
        int count;
        public int Count => count;
        GameObject origin;
        public GameObject Origin => origin;

        public InventoryStack(GameObject origin) : base(origin.ID, stackable:true)
        {
            count = 2;
            this.origin = origin;
            origin.CopyResponsesTo(this);
            origin.Delete();
        }
        InventoryStack(GameObject origin, int count) : base(origin.ID, stackable:true)
        {
            this.count = count;
            this.origin = origin;
            origin.CopyResponsesTo(this);
        }

        public InventoryStack Increment()
        {
            Delete();
            return new InventoryStack(origin, count + 1);
        }
        public GameObject Decrement()
        {
            Delete();
            if (count == 2)
            {
                return origin;
            }
            else
            {
                return new InventoryStack(origin, count - 1);
            }
        }

        public static bool IsStack(GameObject obj)
        {
            return typeof(InventoryStack).IsInstanceOfType(obj);
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
            validity = state;
            missingTargetError = _missingTargetError;
            dipreps = _dipreps;
            aliases = _aliases;
            preps = _preps;
        }
        
        CommandType type;
        public CommandType Type => type;
        string id;
        public string Phrase => id;
        State validity;
        public State Validity => validity;
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
            if (this.validity.Code == 0 && other.validity.Code == 0)
            {
                return true;
            }
            else
            {
                int intersection = this.validity.Code & other.validity.Code;
                return intersection != 0;
            }
        }
    }

    public class Parser
    {
        static Parser parser;
        static public Parser GetParser => parser;
        static public string Clear = Environment.NewLine + Environment.NewLine;
        
        public enum Mode {Standard, Raw}
        Mode mode = Mode.Standard;
        Func<string, string> rawResponse;
        public void GoStandard()
        {
            mode = Mode.Standard;
        }
        public void GoRaw(Func<string, string> response)
        {
            mode = Parser.Mode.Raw;
            rawResponse = response;
        }

        World world;
        string afterword = null;

        public Parser(World _world)
        {
            world = _world;
            parser = this;
        }
        
        public string Parse(string input)
        {
            input = input.ToLower();

            if (String.IsNullOrWhiteSpace(input))
            {
                return "Please type a command.";
            }
            string response;

            switch (mode)
            {
                case Mode.Standard:
                    response = StandardParse(input);
                    break;
                case Mode.Raw:
                    response = RawParse(input);
                    break;
                default:
                    throw new Exception("Unexpected Parser.Mode value.");
            }

            if (afterword != null)
            {
                response += "\n\n" + afterword;
                afterword = null;
            }

            return response;
        }

        string RawParse(string input)
        {
            return rawResponse(input);
        }

        string StandardParse(string input)
        {
            string[] words = (input.Contains(" "))?
                input.Split(" ", StringSplitOptions.RemoveEmptyEntries) : new string[]{input};

            Tuple<Command, int> found = FindCommand(words);
            
            if (found == null)
            {
                return "Your last input didn't contain an action you can take right now.";
            }
            else
            {
                Command cmd = found.Item1;
                int head = found.Item2;
                return HandleType(cmd, words, head);
            }
        }

        Tuple<Command, int> FindCommand(string[] words)
        {
            Tuple<Command, int> found = null;
            string commandString = "";
            for (int i = 0; i < words.Length; i++)
            {
                commandString += words[i];
                Command cmd = world.GetCommand(commandString);
                if (cmd != null)
                {
                    found = new Tuple<Command, int>(cmd, i+1);
                }
                commandString += " ";
            }
            return found;
        }

        string HandleType(Command cmd, string[] words, int head)
        {
            switch (cmd.Type)
            {
                case CommandType.Intransitive:
                    return HandleIntransitive(cmd, words, head);
                case CommandType.Transitive:
                    return HandleTransitive(cmd, words, head);
                case CommandType.Ditransitive:
                    return HandleDitransitive(cmd, words, head);
                default:
                    throw new Exception("Unexpected CommandType value.");
            }
        }

        string HandleIntransitive(Command cmd, string[] words, int head)
        {
            if (head < words.Length)
            {
                return cmd.Phrase + " doesn't take a target.";
            }
            else
            {
                return world.GetIntransitiveResponse(cmd)();
            }
        }

        string HandleTransitive(Command cmd, string[] words, int head)
        {
            if (cmd.Preps != null)
            {
                foreach (string prep in cmd.Preps)
                {
                    if (prep == words[head])
                    {
                        head ++;
                    }
                }
            }
            if (head >= words.Length)
            {
                return cmd.MissingTargetError;
            }
            else
            {
                if (words.Length - head > 1)
                {
                    if (words[head] == "the" || words[head] == "a" || words[head] == "an")
                    {
                        head ++;
                    }
                }
                string objID = String.Join(" ", Subarray(words, head));
                return world.GetTransitiveResponse(cmd)(objID);
            }
        }

        string HandleDitransitive(Command cmd, string[] words, int head)
        {
            //Make sure there are words after the command
            if (head >= words.Length)
            {
                return cmd.MissingTargetError;
            }
            else
            {
                //Split the list of words at diprep's position
                int diprepIndex = 0;
                string obj1ID = "";
                string obj2ID = "";

                foreach (string diprep in cmd.Dipreps)
                {
                    for (int i = head; i < words.Length; i++)
                    {
                        if (diprep == words[i])
                        {
                            diprepIndex = i;
                            if (diprepIndex != head) //If there are words inbetween the end of the command and the diprep
                            {
                                if (diprepIndex - head > 1)
                                {
                                    if (words[head] == "the" || words[head] == "a" || words[head] == "an")
                                    {
                                        head ++;
                                    }
                                }
                                obj1ID = String.Join(" ", Subarray(words, head, diprepIndex));
                            }
                            if (diprepIndex < words.Length) //If there are words after the diprep
                            {
                                int firstAfterPrep = diprepIndex + 1;
                                if (words.Length - firstAfterPrep > 1)
                                {
                                    if (words[firstAfterPrep] == "the" || words[firstAfterPrep] == "a" || words[firstAfterPrep] == "an")
                                    {
                                        firstAfterPrep ++;
                                    }
                                }
                                obj2ID = String.Join(" ", Subarray(words, firstAfterPrep));
                            }
                        }
                    }
                }
                
                //If no diprep, treat the whole remaining string as obj1
                if (diprepIndex == 0)
                {
                    if (words.Length - head > 1)
                    {
                        if (words[head] == "the" || words[head] == "a" || words[head] == "an")
                        {
                            head ++;
                        }
                    }
                    obj1ID = String.Join(" ", Subarray(words, head));
                    return world.GetDitransitiveResponse(cmd)(obj1ID, "");
                }
                else
                {
                    if (obj1ID == "")
                    {
                        return cmd.MissingTargetError + " " + String.Join(" ", Subarray(words, diprepIndex));
                    }

                    Func<string, string, string> response = world.GetDitransitiveResponse(cmd);
                    if (obj2ID == "")
                    {
                        return response(obj1ID, "");
                    }
                    else
                    {
                        return response(obj1ID, obj2ID);
                    }
                }
            }
        }

        string[] Subarray(string[] remainder, int start, int end = 0)
        {
            end = end == 0 ? remainder.Length : end;
            string[] phrase = new string[end - start];
            for (int i = start; i < end; i++)
            {
                phrase[i-start] = remainder[i];
            }
            return phrase;
        }

        public void AddAfterword(string aw)
        {
            afterword = aw;
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

        public static string CapitalizeFirst(string str)
        {
            return str.Substring(0, 1).ToUpper() + str.Substring(1);
        }
    }
}