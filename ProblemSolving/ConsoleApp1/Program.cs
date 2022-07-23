﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
#if DEBUG // delete
using System.Net.Http;
using Newtonsoft.Json;
using WinFormsLibrary1;
using System.Diagnostics;
#endif

namespace ConsoleApp1
{
    public class Program
    {
#if DEBUG // delete
        [STAThread]
#endif
        public static int Main(string[] args)
        {
            using var io = new IoInstance();
#if DEBUG // delete
            var problemNumber = "17081";
            var inputOutputList = BojUtils.MakeInputOutput(problemNumber, useLocalInput: false);
            var checkAll = true;
            foreach (var inputOutput in inputOutputList)
            {
                inputOutput.Input = inputOutput.Input.Replace("&amp;", "&");
                IO.SetInputOutput(inputOutput);
#endif
                var result = Solve();
                result.Dump();
#if DEBUG // delete
                var correct = IO.IsCorrect().Dump();
                checkAll = checkAll && correct;
                Console.WriteLine();
            }
            DebugUtils.CopyCode();
            if (checkAll)
            {
                var url = $"https://www.acmicpc.net/submit/{problemNumber}";
                Process.Start(new ProcessStartInfo($"powershell", $"start chrome {url}"));
            }
#endif
            return 0;
        }

        public static string Solve()
        {
            var outputResult = new List<string>();

            var map = CreateMapFromInput(out var moves, out var player);

            int turnCount = 0;
            foreach (var move in moves)
            {
                turnCount++;
                // 여기에 로직을 채운다.
                Position nextPosition = player.Position.GetNext(move);
                if (map.Movable(nextPosition))
                {
                    player.Position = nextPosition;
                }
                var nextCell = map.GetCell(player.Position);
                var result = nextCell.Interact(player);

                if (result.Win)
                {
                    outputResult.Add(map.ToString(player));
                    outputResult.Add($"Passed Turns : {turnCount}");
                    outputResult.Add(player.ToString());
                    outputResult.Add("YOU WIN!");
                    return outputResult.StringJoin(Environment.NewLine);
                }
                else if (result.Dead)
                {
                    // deadby
                    outputResult.Add(map.ToString(player: null));
                    outputResult.Add($"Passed Turns : {turnCount}");
                    outputResult.Add(player.ToString());
                    outputResult.Add($"YOU HAVE BEEN KILLED BY {result.DeadBy}..");
                    return outputResult.StringJoin(Environment.NewLine);
                }
                else
                {
                    // continue
                }
            }

            outputResult.Add(map.ToString(player));
            outputResult.Add($"Passed Turns : {turnCount}");
            outputResult.Add(player.ToString());
            outputResult.Add("Press any key to continue.");
            return outputResult.StringJoin(Environment.NewLine);
        }

        public static IMap CreateMapFromInput(out List<MoveType> moves, out IPlayer player)
        {
            var (height, width) = IO.GetIntTuple2();
            var lines = height.MakeList(_ => IO.GetLine());
            var map = IMap.Create(height, width, lines);

            moves = IO.GetLine().Select(chr => Ex.ToMoveType(chr)).ToList();

            var monsters = map.MonsterCount.MakeList(_ =>
            {
                var arr = IO.GetStringList();
                var row = arr[0].ToInt() - 1;
                var column = arr[1].ToInt() - 1;
                var name = arr[2];
                var w = arr[3].ToInt();
                var a = arr[4].ToInt();
                var h = arr[5].ToInt();
                var e = arr[6].ToInt();

                var position = new Position(row, column);
                var monster = new Monster(name, w, a, h, e);

                return (position, monster);
            });
            var items = map.ItemCount.MakeList(_ =>
            {
                var arr = IO.GetStringList();
                var row = arr[0].ToInt() - 1;
                var column = arr[1].ToInt() - 1;

                var t = arr[2];
                var s = arr[3];

                var position = new Position(row, column);
                var item = new ItemBox(t, s);

                return (position, item);
            });

            map.UpdateMonsters(monsters);
            map.UpdateItems(items);

            var playerRow = lines.Select((line, row) => (line, row))
                .First(x => x.line.Contains('@'))
                .row;
            var playerColumn = lines[playerRow].IndexOf('@');
            player = new Player
            {
                Position = new Position(playerRow, playerColumn),
                BeginningPosition = new Position(playerRow, playerColumn),
            };

            return map;
        }
    }

    public enum MoveType
    {
        Left, Right, Up, Down
    }

    public record Position
    {
        public int Row { get; init; }
        public int Column { get; init; }
        public Position(int row, int column)
            => (Row, Column) = (row, column);
        public void Deconstruct(out int row, out int column)
            => (row, column) = (Row, Column);
    }

    #region Map
    public interface IMap
    {
        (int Height, int Width) Size { get; }
        // IEnumerable<IEnumerable<ICell>> Cells { get; }

        bool Movable(Position position);
        ICell GetCell(Position position);

        int MonsterCount { get; }
        int ItemCount { get; }

        void UpdateMonsters(List<(Position, Monster)> monsters);
        void UpdateItems(List<(Position, ItemBox)> items);

        string ToString();

        string ToString(IPlayer player);

        static IMap Create(int height, int width, List<string> input)
        {
            return new Map(height, width, input);
        }
    }

    public class Map : IMap
    {
        public Map(int height, int width, List<string> input)
        {
            Size = (height, width);
            _cells = input
                .Select((line, row) => line.Select((chr, column) => ICell.Create(row, column, chr)).ToList())
                .ToList();
        }

        public (int Height, int Width) Size { get; set; }

        public int MonsterCount => _cells
                .Sum(cells => cells.Count(cell => cell.Interactable is IMonster));

        public int ItemCount => _cells
                .Sum(cells => cells.Count(cell => cell.Interactable is IItemBox));

        private List<List<ICell>> _cells;

        public ICell GetCell(Position position)
        {
            return _cells[position.Row][position.Column];
        }

        public bool Movable(Position position)
        {
            if (position.Row < 0 || position.Column < 0)
                return false;
            if (position.Row >= Size.Height || position.Column >= Size.Width)
                return false;

            var cell = _cells[position.Row][position.Column];
            if (cell.Interactable is Wall)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public override string ToString()
        {
            return ToString(null);
        }

        public string ToString(IPlayer player)
        {
            return _cells
                .Select(row => row.Select(cell =>
                {
                    if (cell.Position == player?.Position)
                    {
                        return "@";
                    }
                    else
                    {
                        return cell.ToText();
                    }
                }).StringJoin(""))
                .StringJoin(Environment.NewLine);
        }

        public void UpdateMonsters(List<(Position, Monster)> monsters)
        {
            monsters.ForEach(data =>
            {
                var (position, monster) = data;
                if (_cells[position.Row][position.Column].Interactable is Monster prevMonster)
                {
                    monster.IsBoss = prevMonster?.IsBoss ?? false;
                }
                _cells[position.Row][position.Column].Interactable = monster;
            });
        }

        public void UpdateItems(List<(Position, ItemBox)> items)
        {
            items.ForEach(data =>
            {
                var (position, itembox) = data;
                _cells[position.Row][position.Column].Interactable = itembox;
            });
        }
    }

    public interface ICell
    {
        Position Position { get; init; }

        IInteractable Interactable { get; set; }

        InteractResult Interact(IPlayer player);

        static ICell Create(int row, int column, char chr)
        {
            return new Cell(new Position(row, column), chr);
        }
    }

    public class Cell : ICell
    {
        public Position Position { get; init; }
        public IInteractable Interactable { get; set; }

        public InteractResult Interact(IPlayer player)
        {
            var result = Interactable.Interact(player);
            if (result.ChangeToBlank)
            {
                Interactable = IInteractable.CreateBlank();
            }

            return result;
        }

        public Cell(Position position, char chr)
        {
            Position = position;
            Interactable = IInteractable.Create(chr);
        }
    }
    #endregion

    #region Interatable
    public interface IInteractable
    {
        InteractResult Interact(IPlayer player);
        static IInteractable Create(char chr)
        {
            return chr switch
            {
                '.' => new Blank(),
                '#' => new Wall(),
                'B' => new ItemBox(chr),
                '&' => new Monster(chr),
                'M' => new Monster(chr),
                '^' => new Trap(),
                _ => new Blank(),
            };
        }
        static IInteractable CreateBlank()
        {
            return new Blank();
        }
    }

    public interface IBlank : IInteractable
    {

    }

    public class Blank : IBlank
    {
        public InteractResult Interact(IPlayer player)
        {
            return InteractResult.CreateNoImpactResult();
        }
    }

    public interface IWall : IInteractable
    {
    }

    public class Wall : IWall
    {
        public InteractResult Interact(IPlayer player)
        {
            return InteractResult.CreateNoImpactResult();
        }
    }

    public interface IItemBox : IInteractable
    {
        public IItem Item { get; init; }
    }

    public class ItemBox : IItemBox
    {
        public IItem Item { get; init; }
        
        public ItemBox(string type, string property)
        {
            Item = type switch
            {
                "W" => new Weapon(property.ToInt()),
                "A" => new Armor(property.ToInt()),
                "O" => IOrnament.Create(property),
                _ => throw new ArgumentException(),
            };
        }
        public ItemBox(char chr)
        {
        }

        public InteractResult Interact(IPlayer player)
        {
            return Item.Interact(player);
        }
    }

    public interface IMonster : IInteractable
    {
        bool IsBoss { get; }
        string Name { get; }
        int AttackValue { get; }
        int DefenseValue { get; }
        int MaxHP { get; }
        int CurrentHP { get; }
        int Experience { get; }
    }

    public class Monster : IMonster
    {
        public bool IsBoss { get; set; }
        public string Name { get; set; }
        public int AttackValue { get; set; }
        public int DefenseValue { get; set; }
        public int MaxHP { get; set; }
        public int CurrentHP { get; set; }
        public int Experience { get; set; }

        public Monster(string name, int weapon, int armor, int hp, int exp)
        {
            Name = name;
            AttackValue = weapon;
            DefenseValue = armor;
            MaxHP = hp;
            CurrentHP = hp;
            Experience = exp;
        }
        public Monster(char chr)
        {
            if (chr == 'M')
            {
                IsBoss = true;
            }
        }
        public Monster() { }

        public InteractResult Interact(IPlayer player)
        {
            bool monsterDead;
            bool playerDead = false;

            // 플레이어의 첫 번째 공격
            if (player.OwnOrnamentOfType(typeof(OrnamentCourage)))
            {
                if (player.OwnOrnamentOfType(typeof(OrnamentDexterity)))
                {
                    monsterDead = SufferDamage(player.TotalAttackValue * 3);
                }
                else
                {
                    monsterDead = SufferDamage(player.TotalAttackValue * 2);
                }
            }
            else
            {
                monsterDead = SufferDamage(player.TotalAttackValue);
            }

            if (monsterDead)
            {
                return FinalizeMatch(playerDead, player);
            }

            // 상대방이 보스일 경우, 몬스터의 첫 번째 공격이 특수성을 가짐
            if (player.OwnOrnamentOfType(typeof(OrnamentHunter)) && IsBoss is true)
            {
                // 체력 회복하고, 보스로부터 공격 당해도 피가 닳지 않음
                player.RecoverFullHP();
                
                // 플레이어부터 공격해서 일방이 죽을 때까지 전투
                while (true)
                {
                    monsterDead = SufferDamage(player.TotalAttackValue);
                    if (monsterDead) break;
                    playerDead = player.SufferDamage(AttackValue);
                    if (playerDead) break;
                }
                return FinalizeMatch(playerDead, player);                
            }
            else
            {
                // 몬스터부터 공격해서 일방이 죽을 때까지 전투
                while (true)
                {
                    playerDead = player.SufferDamage(AttackValue);
                    if (playerDead) break;
                    monsterDead = SufferDamage(player.TotalAttackValue);
                    if (monsterDead) break;
                }
                return FinalizeMatch(playerDead, player);
            }
        }

        private InteractResult FinalizeMatch(bool playerDead, IPlayer player)
        {
            if (playerDead)
            {
                if (player.OwnOrnamentOfType(typeof(OrnamentReincarnation)))
                {
                    CurrentHP = MaxHP;

                    player.Reincarnate();

                    return InteractResult.CreateNoImpactResult();
                }
                else
                {
                    return InteractResult.CreateDeadResult(Name);
                }
            }
            else
            {
                player.RecoverHPWithOrnament();
                player.GainExperience(Experience);

                return IsBoss ?
                    InteractResult.CreateWinResult() :
                    InteractResult.CreateChangeToBlankResult();
            }
        }

        // return: 데미지를 입어 죽을 경우 true 반환
        private bool SufferDamage(int damage)
        {
            CurrentHP -= Math.Max(1, damage - DefenseValue);
            return CurrentHP <= 0;
        }
    }

    public interface ITrap : IInteractable
    {
        public int Damage { get; init; }
    }

    public class Trap : ITrap
    {
        public int Damage { get; init; } = 5;
        private const string TRAPNAME = "SPIKE TRAP";
        
        public InteractResult Interact(IPlayer player)
        {
            bool ownDexterity = player.OwnOrnamentOfType(typeof(OrnamentDexterity));

            if (ownDexterity)
            {
                player.DecreaseHP(1);
            }
            else
            {
                player.DecreaseHP(Damage);
            }

            InteractResult result;
            if (player.CurrentHP <= 0)
            {
                bool ownReincarnation = player.OwnOrnamentOfType(typeof(OrnamentReincarnation));
                if (ownReincarnation)
                {
                    player.Reincarnate();
                    result = InteractResult.CreateNoImpactResult();
                }
                else
                {
                    result = InteractResult.CreateDeadResult(TRAPNAME);
                }
            }
            else
            {
                result = InteractResult.CreateNoImpactResult();
            }

            return result;
        }
    }

    public class InteractResult
    {
        public bool Dead { get; init; } // 부활까지 포함해서 최종적으로 죽었는지
        public bool ChangeToBlank { get; init; }
        public bool Win { get; init; }
        public string DeadBy { get; init; }

        private InteractResult() { }

        public static InteractResult CreateWinResult()
        {
            return new InteractResult { Win = true };
        }
        public static InteractResult CreateDeadResult(string deadBy)
        {
            return new InteractResult
            {
                Dead = true,
                DeadBy = deadBy,
            };
        }

        public static InteractResult CreateNoImpactResult()
        {
            return new InteractResult();
        }

        public static InteractResult CreateChangeToBlankResult()
        {
            return new InteractResult { ChangeToBlank = true };
        }
    }
    #endregion

    #region Player
    public interface IPlayer
    {
        Position Position { get; set; }
        Position BeginningPosition { get; init; }

        int Experience { get; set; }
        int Level { get; set; }

        int MaxHP { get; set; }
        int CurrentHP { get; set; }

        int BareAttackValue { get; set; }
        int TotalAttackValue { get; }
        int BareDefenseValue { get; set; }

        IWeapon Weapon { get; set; }
        IArmor Armor { get; set; }
        IOrnament[] Ornaments { get; set; }

        string ToString();
        void DecreaseHP(int damage);
        bool OwnOrnamentOfType(Type ornamentType);
        void Reincarnate();
        void RecoverFullHP();
        void RecoverHPWithOrnament();
        void GainExperience(int exp);
        bool SufferDamage(int damage);
    }

    public class Player : IPlayer
    {
        private const int ORNAMENTCAPA = 4;
        private const int LEVELUPMULTIPLE = 5;
        
        public Position Position { get; set; }
        public Position BeginningPosition { get; init; }
        public int Experience { get; set; } = 0;
        public int Level { get; set; } = 1;
        public int MaxHP { get; set; } = 20;
        public int CurrentHP { get; set; } = 20;
        public int BareAttackValue { get; set; } = 2;
        public int TotalAttackValue { get => BareAttackValue + (Weapon?.AttackValue ?? 0); }
        public int BareDefenseValue { get; set; } = 2;
        public int TotalDefenseValue { get => BareDefenseValue + (Armor?.DefenseValue ?? 0); }
        public IWeapon Weapon { get; set; }
        public IArmor Armor { get; set; }
        public IOrnament[] Ornaments { get; set; } = new IOrnament[ORNAMENTCAPA];

        public override string ToString()
        {
            return
                $"LV : {Level}\n" +
                $"HP : {CurrentHP}/{MaxHP}\n" +
                $"ATT : {BareAttackValue}+{Weapon?.AttackValue ?? 0}\n" +
                $"DEF : {BareDefenseValue}+{Armor?.DefenseValue ?? 0}\n" +
                $"EXP : {Experience}/{Level * LEVELUPMULTIPLE}";
        }

        public void DecreaseHP(int damage)
        {
            CurrentHP -= damage;
        }

        private void GoBackToBeinningPosition()
        {
            Position = new Position(BeginningPosition.Row, BeginningPosition.Column);
        }

        public void RecoverFullHP()
        {
            CurrentHP = MaxHP;
        }

        public bool OwnOrnamentOfType(Type ornamentType)
        {
            return Ornaments.Any(o =>
            {
                if (o is null) return false;
                else return o.GetType().Equals(ornamentType);
            });
        }
        
        private int IndexOfOrnamentIfPossess(Type ornamentType)
        {
            return Array.FindIndex(Ornaments, o =>
            {
                if (o is null) return false;
                else return o.GetType().Equals(ornamentType);
            });
        }

        private void RemoveOrnament(int ornamentIndex)
        {
            Ornaments[ornamentIndex] = null;
        }

        public void Reincarnate()
        {
            RecoverFullHP();
            GoBackToBeinningPosition();
            int ornamentIndex = IndexOfOrnamentIfPossess(typeof(OrnamentReincarnation));
            RemoveOrnament(ornamentIndex);
        }

        public void RecoverHPWithOrnament()
        {
            // HP Regeneration Ornament가 있을 경우 HP 회복
            int ornamentIndex = IndexOfOrnamentIfPossess(typeof(OrnamentHpRegeneration));
            if (ornamentIndex != -1)
            {
                int hpRegenCapa = ((OrnamentHpRegeneration)Ornaments[ornamentIndex]).HPRegenCapa;
                CurrentHP = Math.Min(CurrentHP + hpRegenCapa, MaxHP);
            }
        }

        public void GainExperience(int exp)
        {
            int ornamentIndex = IndexOfOrnamentIfPossess(typeof(OrnamentExperience));
            if (ornamentIndex != -1)
            {
                var experienceMultiple = ((OrnamentExperience)Ornaments[ornamentIndex]).ExperienceMultiple;
                exp = Convert.ToInt32(exp * experienceMultiple);
            }
            Experience += exp;
            if (Experience >= Level * LEVELUPMULTIPLE) LevelUp();
        }

        private void LevelUp()
        {
            Level += 1;
            Experience = 0;
            MaxHP += 5;
            BareAttackValue += 2;
            BareDefenseValue += 2;
            
            RecoverFullHP();
        }

        // return: 데미지를 입어 죽을 경우 true 반환
        public bool SufferDamage(int damage)
        {
            CurrentHP -= Math.Max(1, damage - TotalDefenseValue);
            if (CurrentHP <= 0) 
            {
                CurrentHP = 0;
                return true;
            }

            return false;
        }
    }
    #endregion

    #region Item
    public interface IItem : IInteractable
    {
    }

    public interface IWeapon : IItem
    {
        public int AttackValue { get; init;}
    }

    public class Weapon : IWeapon
    {
        public int AttackValue { get; init; }

        public Weapon(int attackValue)
        {
            AttackValue = attackValue;
        }

        public InteractResult Interact(IPlayer player)
        {
            player.Weapon = this;
            
            return InteractResult.CreateChangeToBlankResult();
        }
    }

    public interface IArmor : IItem
    {
        public int DefenseValue { get; init; }
    }

    public class Armor : IArmor
    {
        public int DefenseValue { get; init; }

        public Armor(int defenseValue)
        {
            DefenseValue = defenseValue;
        }

        public InteractResult Interact(IPlayer player)
        {
            player.Armor = this;
            return InteractResult.CreateChangeToBlankResult();
        }
    }

    public interface IOrnament : IItem
    {
        static IOrnament Create(string property)
        {
            return property switch
            {
                "HR" => new OrnamentHpRegeneration(),
                "RE" => new OrnamentReincarnation(),
                "CO" => new OrnamentCourage(),
                "EX" => new OrnamentExperience(),
                "DX" => new OrnamentDexterity(),
                "HU" => new OrnamentHunter(),
                "CU" => new OrnamentCursed(),
                _ => throw new ArgumentException(),
            };
        }

        static void EquipOrnament(IOrnament ornament, IPlayer player)
        {
            if (IsOrnamentsFull(player))
            {
                return;
            }

            if (IsDuplicateOrnament(ornament, player))
            {
                return;
            }

            AddOrnament(ornament, player);
        }

        private static bool IsOrnamentsFull(IPlayer player)
        {
            return player.Ornaments.All(o => o is not null);
        }

        private static bool IsDuplicateOrnament(IOrnament ornament, IPlayer player)
        {
            return player.Ornaments.Any(o => o?.GetType() == ornament.GetType());
        }

        private static void AddOrnament(IOrnament ornament, IPlayer player)
        {
            for (int i = 0; i < player.Ornaments.Length; i++)
            {
                if (player.Ornaments[i] is null)
                {
                    player.Ornaments[i] = ornament;
                    break;
                }
            }
        }
    }

    public class OrnamentHpRegeneration : IOrnament
    {
        public int HPRegenCapa { get; init; } = 3;
        
        public InteractResult Interact(IPlayer player)
        {
            IOrnament.EquipOrnament(this, player);
            return InteractResult.CreateChangeToBlankResult();
        }
    }

    public class OrnamentReincarnation : IOrnament
    {
        public InteractResult Interact(IPlayer player)
        {
            IOrnament.EquipOrnament(this, player);
            return InteractResult.CreateChangeToBlankResult();
        }
    }

    public class OrnamentCourage : IOrnament
    {
        public InteractResult Interact(IPlayer player)
        {
            IOrnament.EquipOrnament(this, player);
            return InteractResult.CreateChangeToBlankResult();
        }
    }

    public class OrnamentExperience : IOrnament
    {
        public double ExperienceMultiple { get; init; } = 1.2;
        public InteractResult Interact(IPlayer player)
        {
            IOrnament.EquipOrnament(this, player);
            return InteractResult.CreateChangeToBlankResult();
        }
    }

    public class OrnamentDexterity : IOrnament
    {
        public InteractResult Interact(IPlayer player)
        {
            IOrnament.EquipOrnament(this, player);
            return InteractResult.CreateChangeToBlankResult();
        }
    }

    public class OrnamentHunter : IOrnament
    {
        public InteractResult Interact(IPlayer player)
        {
            IOrnament.EquipOrnament(this, player);
            return InteractResult.CreateChangeToBlankResult();
        }
    }

    public class OrnamentCursed : IOrnament
    {
        public InteractResult Interact(IPlayer player)
        {
            IOrnament.EquipOrnament(this, player);
            return InteractResult.CreateChangeToBlankResult();
        }
    }
    #endregion

    #region Ex
    public static partial class Ex
    {
        public static string ToText(this ICell cell)
        {
            switch (cell.Interactable)
            {
                case IBlank: return ".";
                case IWall: return "#";
                case IItemBox: return "B";
                case IMonster monster: return monster.IsBoss ? "M" : "&";
                case ITrap: return "^";
                default: return ".";
            }
        }

        public static MoveType ToMoveType(char chr)
        {
            return chr switch
            {
                'L' => MoveType.Left,
                'R' => MoveType.Right,
                'U' => MoveType.Up,
                'D' => MoveType.Down,
                _ => throw new Exception($"LRUD말고 다른 값이 들어왔음: {chr}"),
            };
        }

        public static Position GetNext(this Position position, MoveType move)
        {
            return move switch
            {
                MoveType.Left => new Position(position.Row, position.Column - 1),
                MoveType.Right => new Position(position.Row, position.Column + 1),
                MoveType.Up => new Position(position.Row - 1, position.Column),
                MoveType.Down => new Position(position.Row + 1, position.Column),
                _ => throw new ArgumentException(),
            };
        }
    }
    #endregion

    #region IO
    public class IoInstance : IDisposable
    {
        public void Dispose()
        {
#if !DEBUG
            IO.Dispose();
#endif
        }
    }

    public static class IO
    {
#if DEBUG // delete
        static List<string> _input = new();
        static string _answer;
        static string _output = "";
        static int _readInputCount = 0;

        public static bool IsCorrect() => _output.Replace("\r", "").Trim() == _answer.Replace("\r", "").Trim();
        public static int InputCount => _input.Count();

        static IO()
        {
            _input = File.ReadAllLines("input.txt", Encoding.UTF8).ToList();
            _answer = File.ReadAllText("output.txt", Encoding.UTF8);
            Init();
        }

        public static void Init()
        {
            _output = "";
            _readInputCount = 0;
        }

        public static void SetInputOutput(InputOutput inputOutput)
        {
            Init();
            _input = inputOutput.Input.Replace("\r", "").Split('\n').ToList();
            _answer = inputOutput.Output;
        }
#endif

#if !DEBUG
        static StreamReader _inputReader;
        static StringBuilder _outputBuffer = new();

        static IO()
        {
            _inputReader = new StreamReader(new BufferedStream(Console.OpenStandardInput()));
        }
#endif

        public static string GetLine()
        {
#if DEBUG
            return _input[_readInputCount++];
#else
            return _inputReader.ReadLine();
#endif
        }

        public static string GetString()
            => GetLine();

        public static string[] GetStringList()
            => GetLine().Split(' ');

        public static (string, string) GetStringTuple2()
        {
            var arr = GetStringList();
            return (arr[0], arr[1]);
        }

        public static (string, string, string) GetStringTuple3()
        {
            var arr = GetStringList();
            return (arr[0], arr[1], arr[2]);
        }

        public static (string, string, string, string) GetStringTuple4()
        {
            var arr = GetStringList();
            return (arr[0], arr[1], arr[2], arr[3]);
        }

        public static int[] GetIntList()
            => GetLine().Split(' ').Where(x => x.Length > 0).Select(x => x.ToInt()).ToArray();

        public static (int, int) GetIntTuple2()
        {
            var arr = GetLine().Split(' ');
            return (arr[0].ToInt(), arr[1].ToInt());
        }

        public static (int, int, int) GetIntTuple3()
        {
            var arr = GetLine().Split(' ');
            return (arr[0].ToInt(), arr[1].ToInt(), arr[2].ToInt());
        }

        public static (int, int, int, int) GetIntTuple4()
        {
            var arr = GetLine().Split(' ');
            return (arr[0].ToInt(), arr[1].ToInt(), arr[2].ToInt(), arr[3].ToInt());
        }

        public static int GetInt()
            => GetLine().ToInt();

        public static long[] GetLongList()
            => GetLine().Split(' ').Where(x => x.Length > 0).Select(x => x.ToLong()).ToArray();

        public static (long, long) GetLongTuple2()
        {
            var arr = GetLine().Split(' ');
            return (arr[0].ToLong(), arr[1].ToLong());
        }

        public static (long, long, long) GetLongTuple3()
        {
            var arr = GetLine().Split(' ');
            return (arr[0].ToLong(), arr[1].ToLong(), arr[2].ToLong());
        }

        public static (long, long, long, long) GetLongTuple4()
        {
            var arr = GetLine().Split(' ');
            return (arr[0].ToLong(), arr[1].ToLong(), arr[2].ToLong(), arr[3].ToLong());
        }

        public static long GetLong()
            => GetLine().ToLong();

        public static T Dump<T>(this T obj, string format = "")
        {
            var text = string.IsNullOrEmpty(format) ? $"{obj}" : string.Format(format, obj);
#if DEBUG // delete
            Console.WriteLine(text);
            _output += Environment.NewLine + text;
#endif
#if !DEBUG
            _outputBuffer.AppendLine(text);
#endif
            return obj;
        }

        public static List<T> Dump<T>(this List<T> list)
        {
#if DEBUG // delete
            Console.WriteLine(list.StringJoin(" "));
#endif
#if !DEBUG
            _outputBuffer.AppendLine(list.StringJoin(" "));
#endif
            return list;
        }

#if !DEBUG
        public static void Dispose()
        {
            _inputReader.Close();
            using var streamWriter = new StreamWriter(new BufferedStream(Console.OpenStandardOutput()));
            streamWriter.Write(_outputBuffer.ToString());
        }
#endif
    }
    #endregion

    #region Extensions
    public enum LoopResult
    {
        Void,
        Break,
        Continue,
    }

    public static class Extensions
    {
        public static string With(this string format, params object[] obj)
        {
            return string.Format(format, obj);
        }

        public static string StringJoin<T>(this IEnumerable<T> list, string separator = " ")
        {
            return string.Join(separator, list);
        }

        public static string Left(this string value, int length = 1)
        {
            if (value.Length < length)
                return value;
            return value.Substring(0, length);
        }

        public static string Right(this string value, int length = 1)
        {
            if (value.Length < length)
                return value;
            return value.Substring(value.Length - length, length);
        }


        public static int ToInt(this string str)
        {
            return int.Parse(str);
        }

        public static long ToLong(this string str)
        {
            return long.Parse(str);
        }


        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
                action(item);
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            var index = 0;
            foreach (var item in source)
                action(item, index++);
        }

        public static void ForEach1<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            var index = 1;
            foreach (var item in source)
                action(item, index++);
        }

        public static void ForEach<T>(this IEnumerable<T> source, Func<T, LoopResult> action)
        {
            foreach (var item in source)
            {
                var result = action(item);
                switch (result)
                {
                    case LoopResult.Break:
                        break;
                    case LoopResult.Continue:
                        continue;
                }
            }
        }

        public static void ForEach<T>(this IEnumerable<T> source, Func<T, int, LoopResult> action)
        {
            var index = 0;
            foreach (var item in source)
            {
                var result = action(item, index++);
                switch (result)
                {
                    case LoopResult.Break:
                        break;
                    case LoopResult.Continue:
                        continue;
                }
            }
        }

        public static void ForEach1<T>(this IEnumerable<T> source, Func<T, int, LoopResult> action)
        {
            var index = 1;
            foreach (var item in source)
            {
                var result = action(item, index++);
                switch (result)
                {
                    case LoopResult.Break:
                        break;
                    case LoopResult.Continue:
                        continue;
                }
            }
        }

        public static bool ForEachBool<T>(this IEnumerable<T> source, Func<T, bool> func)
        {
            var result = true;
            foreach (var item in source)
            {
                if (!func(item))
                    result = false;
            }
            return result;
        }

        public static bool ForEachBool<T>(this IEnumerable<T> source, Func<T, int, bool> func)
        {
            var result = true;
            var index = 0;
            foreach (var item in source)
            {
                if (!func(item, index++))
                    result = false;
            }
            return result;
        }

        public static void For(this int count, Action<int> action)
        {
            for (var i = 0; i < count; i++)
            {
                action(i);
            }
        }

        public static void For1(this int count, Action<int> action)
        {
            for (var i = 1; i <= count; i++)
            {
                action(i);
            }
        }

        public static void For(this int count, Func<int, LoopResult> action)
        {
            for (var i = 0; i < count; i++)
            {
                var result = action(i);
                switch (result)
                {
                    case LoopResult.Break:
                        break;
                    case LoopResult.Continue:
                        continue;
                }
            }
        }

        public static void For1(this int count, Func<int, LoopResult> action)
        {
            for (var i = 1; i <= count; i++)
            {
                var result = action(i);
                switch (result)
                {
                    case LoopResult.Break:
                        break;
                    case LoopResult.Continue:
                        continue;
                }
            }
        }

        public static List<T> MakeList<T>(this int count, Func<int, T> func)
        {
            var result = new List<T>();
            for (var i = 0; i < count; i++)
            {
                result.Add(func(i));
            }
            return result;
        }
        public static TResult Reduce<TSource, TResult>(this IEnumerable<TSource> source, TResult initValue, Func<TResult, TSource, TResult> fn)
        {
            return Reduce(source, initValue, (value, item, index, list) => fn(value, item));
        }

        public static TResult Reduce<TSource, TResult>(this IEnumerable<TSource> source, TResult initValue, Func<TResult, TSource, int, TResult> fn)
        {
            return Reduce(source, initValue, (value, item, index, list) => fn(value, item, index));
        }

        public static TResult Reduce<TSource, TResult>(this IEnumerable<TSource> source, TResult initValue, Func<TResult, TSource, int, IEnumerable<TSource>, TResult> fn)
        {
            var value = initValue;

            var index = 0;
            foreach (var item in source)
            {
                value = fn(value, item, index++, source);
            }

            return value;
        }

        public static IEnumerable<(TItem Item1, TItem Item2)> AllPairs<TItem>(this List<TItem> source, bool includeDuplicate = false)
        {
            for (var i = 0; i < source.Count(); i++)
            {
                var item1 = source[i];
                for (var j = i + (includeDuplicate ? 0 : 1); j < source.Count(); j++)
                {
                    var item2 = source[j];
                    yield return (item1, item2);
                }
            }
        }

        public static IEnumerable<(TItem Item1, TItem Item2)> AllPairs<TItem>(this TItem[] source, bool includeDuplicate = false)
        {
            for (var i = 0; i < source.Count(); i++)
            {
                var item1 = source[i];
                for (var j = i + (includeDuplicate ? 0 : 1); j < source.Count(); j++)
                {
                    var item2 = source[j];
                    yield return (item1, item2);
                }
            }
        }

        public static bool Empty<TSource>(this IEnumerable<TSource> source)
        {
            return !source.Any();
        }

        public static bool Empty<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            return !source.Any(predicate);
        }
    }
    #endregion

    #region Utils
#if DEBUG // delete

    public static class DebugUtils
    {
        public static void CopyCode()
        {
            var programDirPath = Environment.CurrentDirectory;
            while (!File.Exists(Path.Combine(programDirPath, "Program.cs")))
            {
                programDirPath = Directory.GetParent(programDirPath).FullName;
            }
            var programPath = Path.Combine(programDirPath, "Program.cs");
            var sourceCode = File.ReadAllText(programPath);

            var filteredCode = sourceCode
                .DeleteDebug().DeleteDebug().DeleteDebug().DeleteDebug().DeleteDebug().DeleteDebug()
                .DeleteDebug().DeleteDebug().DeleteDebug().DeleteDebug().DeleteDebug().DeleteDebug()
                .DeleteDebug().DeleteDebug().DeleteDebug().DeleteDebug().DeleteDebug().DeleteDebug()
                ;

            Helper.CopyText(filteredCode);
        }

        private static string DeleteDebug(this string sourceCode)
        {
            var beginIndex = sourceCode.IndexOf("#if DEBUG // delete");
            if (beginIndex == -1)
                return sourceCode;
            var endIf = "_endif".Replace("_", "#");
            var endIndex = sourceCode.IndexOf(endIf, beginIndex);

            return sourceCode.Substring(0, beginIndex - 1) + sourceCode.Substring(endIndex + 8);
        }
    }

    #region Input Output

    public class InputOutput
    {
        public int Number;
        public string Input;
        public string Output;
    }

    public static class BojUtils
    {
        private static string GetHtml(this string problemNumber)
        {
            var client = new HttpClient();
            var task = client.GetStringAsync($"https://www.acmicpc.net/problem/{problemNumber}");
            task.Wait();
            return task.Result;
        }

        public static List<InputOutput> MakeInputOutput(string problemNumber, bool useLocalInput = false)
        {
            var localInputList = new List<InputOutput>();
            if (useLocalInput)
            {
                var inputText = File.ReadAllText("input.txt", Encoding.UTF8);
                var outputText = File.ReadAllText("output.txt", Encoding.UTF8);

                Func<string, List<string>> Normalize = text =>
                    Regex.Split(text.Replace("\r", ""), "\n\n")
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToList();

                var inputList = Normalize(inputText);
                var outputList = Normalize(outputText);

                if (inputList.Count != outputList.Count)
                {
                    throw new Exception("input, output 개수가 다릅니다.");
                }

                localInputList = inputList
                    .Zip(outputList, (input, output) => new InputOutput
                    {
                        Number = 0,
                        Input = input,
                        Output = output,
                    })
                    .ToList();
            }

            if (AppCache.Exists(problemNumber))
            {
                var cached = AppCache.GetCachedInputOutput(problemNumber);
                if (useLocalInput)
                {
                    cached = localInputList.Concat(cached).ToList();
                }
                return cached;
            }

            var sampleDic = new Dictionary<(string, string), string>();

            var sampleDataPattern = @"class\s*=\s*\""sampledata\""";
            var sampleNumberPattern = @"id\s*=\s*\""sample-(input|output)-(\d+)\""";

            var html = problemNumber.GetHtml();

            var startIndex = 0;
            while (true)
            {
                var a = html.IndexOf("<pre", startIndex);
                if (a == -1)
                    break;
                var b = html.IndexOf(">", a);
                var c = html.IndexOf("</pre>", a);
                var preHeader = html.Substring(a, b - a);
                var preData = html.Substring(b + 1, c - b - 1).Trim();
                preData = preData.Replace("&quot;", "\"");

                // preHeader.Dump();
                // preData.Dump();
                if (!Regex.IsMatch(preHeader, sampleDataPattern))
                {
                    startIndex = a + 1;
                    continue;
                }

                var match = Regex.Match(preHeader, sampleNumberPattern);
                if (match.Success)
                {
                    var type = match.Groups[1].Captures[0].Value;
                    var number = match.Groups[2].Captures[0].Value;
                    // type.Dump();
                    // number.Dump();
                    sampleDic[(type, number)] = preData;
                }

                startIndex = a + 1;
            }
            // sampleDic.Dump();
            var result = sampleDic
                .GroupBy(x => new { Number = x.Key.Item2 })
                .Select(x => new InputOutput
                {
                    Number = int.Parse(x.Key.Number),
                    Input = x.First(e => e.Key.Item1 == "input").Value,
                    Output = x.First(e => e.Key.Item1 == "output").Value,
                })
                .ToList();

            AppCache.SaveInputOutput(problemNumber, result);

            if (useLocalInput)
            {
                result = localInputList.Concat(result).ToList();
            }

            return result;
        }
    }

    public static class AppCache
    {
        private static readonly string DirPath = Environment.CurrentDirectory;
        public static bool Exists(string problemNumber)
        {
            var problemPath = Path.Combine(DirPath, $"{problemNumber}.json");
            return File.Exists(problemPath);
        }

        public static List<InputOutput> GetCachedInputOutput(string problemNumber)
        {
            if (!Exists(problemNumber))
                return null;

            var problemPath = Path.Combine(DirPath, $"{problemNumber}.json");
            var text = File.ReadAllText(problemPath, Encoding.UTF8);
            return JsonConvert.DeserializeObject<List<InputOutput>>(text);
        }

        public static void SaveInputOutput(string problemNumber, List<InputOutput> data)
        {
            if (!Directory.Exists(DirPath))
            {
                Directory.CreateDirectory(DirPath);
            }

            var text = JsonConvert.SerializeObject(data, Formatting.Indented);
            var problemPath = Path.Combine(DirPath, $"{problemNumber}.json");

            File.WriteAllText(problemPath, text, Encoding.UTF8);
        }
    }

    #endregion

#endif
    #endregion
}
