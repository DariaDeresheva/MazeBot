using System;
using System.Collections.Generic;
using System.Linq;
using SpurRoguelike.Core;
using SpurRoguelike.Core.Primitives;
using SpurRoguelike.Core.Views;

namespace SpurRoguelike.PlayerBot
{
    public class PlayerBot : IPlayerController
    {
        private const int CriticalHealth = 51;
        private HashSet<Location> _emptiesOnceWasVisible;
        private Location _exit;
        private HashSet<Location> _hiddenYet;

        public Turn MakeTurn(LevelView level, IMessageReporter messageReporter)
        {
            if (_hiddenYet == default(HashSet<Location>) ||
                level.Field[level.Player.Location] == CellType.PlayerStart)
                _hiddenYet = new HashSet<Location>(level.Field.GetCellsOfType(CellType.Hidden));

            _hiddenYet.IntersectWith(level.Field.GetCellsOfType(CellType.Hidden));

            if (_emptiesOnceWasVisible == default(HashSet<Location>) ||
                level.Field[level.Player.Location] == CellType.PlayerStart)
                _emptiesOnceWasVisible = new HashSet<Location>(level.Field.GetCellsOfType(CellType.Empty));

            _emptiesOnceWasVisible.UnionWith(level.Field.GetCellsOfType(CellType.Empty));
            _emptiesOnceWasVisible.UnionWith(level.Field.GetCellsOfType(CellType.Exit));
            _emptiesOnceWasVisible.UnionWith(level.Field.GetCellsOfType(CellType.PlayerStart));

            if (_exit == default(Location) ||
                level.Field[level.Player.Location] == CellType.PlayerStart)
                _exit = level.Field.GetCellsOfType(CellType.Exit).SingleOrDefault();

            if (level.Player.Health <= CriticalHealth && TryGetBestStepToHealth(level, out var locationToStep))
                return Turn.Step(locationToStep - level.Player.Location);

            var attackRangeMonsters =
                level.Monsters.Where(x => x.Location.IsInRange(level.Player.Location, 1)).ToArray();

            if (attackRangeMonsters.Any())
            {
                if (attackRangeMonsters.Length == 1 && level.Player.Health >= CriticalHealth)
                    return Turn.Attack(
                        attackRangeMonsters
                            .Aggregate((result, current) => result.Health < current.Health ? result : current)
                            .Location - level.Player.Location);

                if (TryGetBestStepAwayFromMonsters(level, out locationToStep))
                    return Turn.Step(locationToStep - level.Player.Location);
            }

            if (level.Monsters.Any() && TryGetBestStepToAttack(level, out locationToStep))
                return Turn.Step(locationToStep - level.Player.Location);

            level.Player.TryGetEquippedItem(out var item);

            if (level.Items.Any(x => x.AttackBonus > item.AttackBonus && x.DefenceBonus > item.DefenceBonus) &&
                TryGetBestStepToItem(level, out locationToStep))
                return Turn.Step(locationToStep - level.Player.Location);

            return Turn.Step(GetBestStepToExit(level) - level.Player.Location);
        }

        private bool TryGetBestStepToAttack(LevelView level, out Location locationToStep)
        {
            locationToStep = FindByDijkstra(level, IsToConsider, GetCost);

            return locationToStep != default(Location);

            bool IsToConsider(Location location)
            {
                return level.GetMonsterAt(location).HasValue;
            }

            int GetCost(Location location)
            {
                if (level.Field[location] == CellType.Wall ||
                    level.Field[location] == CellType.Trap ||
                    level.Field[location] == CellType.Exit ||
                    level.GetItemAt(location).HasValue)
                    return 100;

                if (level.GetMonsterAt(location).HasValue)
                    return 0;

                if (level.Field[location] == CellType.Empty || level.Field[location] == CellType.PlayerStart)
                    return 10 - GetNeighbours(location, NeighbourType.Eight)
                               .Count(x => level.Field[x] == CellType.Trap ||
                                           level.Field[x] == CellType.Wall);

                if (level.Field[location] == CellType.Hidden) return 10;

                throw new ArgumentOutOfRangeException(nameof(location));
            }
        }

        private bool TryGetBestStepToHealth(LevelView level, out Location locationToStep)
        {
            locationToStep = FindByDijkstra(level, IsToConsider, GetCost);

            return locationToStep != default(Location);

            bool IsToConsider(Location location)
            {
                return level.GetHealthPackAt(location).HasValue ||
                       location == _exit && _exit != default(Location) ||
                       GetNeighbours(location, NeighbourType.Four)
                           .Any(y => level.Field[y] == CellType.Hidden && _hiddenYet.Contains(y));
            }

            int GetCost(Location location)
            {
                if (level.Field[location] == CellType.Wall ||
                    level.Field[location] == CellType.Trap ||
                    level.GetMonsterAt(location).HasValue ||
                    level.GetItemAt(location).HasValue)
                    return 100;

                if (level.Field[location] == CellType.Exit ||
                    level.GetHealthPackAt(location).HasValue)
                    return 0;

                if (level.Field[location] == CellType.Empty || level.Field[location] == CellType.PlayerStart)
                    return 10 - GetNeighbours(location, NeighbourType.Eight)
                               .Count(x => level.Field[x] == CellType.Trap ||
                                           level.Field[x] == CellType.Wall);

                if (level.Field[location] == CellType.Hidden) return 10;

                throw new ArgumentOutOfRangeException(nameof(location));
            }
        }

        private bool TryGetBestStepAwayFromMonsters(LevelView level, out Location locationToStep)
        {
            locationToStep = FindByDijkstra(level, IsToConsider, GetCost);

            return locationToStep != default(Location);

            bool IsToConsider(Location location)
            {
                return level.GetHealthPackAt(location).HasValue ||
                       location == _exit && _exit != default(Location) ||
                       GetNeighbours(location, NeighbourType.Four)
                           .Any(y => level.Field[y] == CellType.Hidden && _hiddenYet.Contains(y));
            }

            int GetCost(Location location)
            {
                if (level.Field[location] == CellType.Wall ||
                    level.Field[location] == CellType.Trap ||
                    level.GetMonsterAt(location).HasValue ||
                    level.GetItemAt(location).HasValue)
                    return 100;

                if (level.Field[location] == CellType.Exit) return 0;

                if (level.Field[location] == CellType.Empty || level.Field[location] == CellType.PlayerStart)
                    return 10 - GetNeighbours(location, NeighbourType.Eight)
                               .Count(x => level.Field[x] == CellType.Trap ||
                                           level.Field[x] == CellType.Wall);

                if (level.Field[location] == CellType.Hidden) return 10;

                throw new ArgumentOutOfRangeException(nameof(location));
            }
        }

        private bool TryGetBestStepToItem(LevelView level, out Location locationToStep)
        {
            locationToStep = FindByDijkstra(level, IsToConsider, GetCost);

            return locationToStep != default(Location);

            bool IsToConsider(Location location)
            {
                level.Player.TryGetEquippedItem(out var item);

                return level.GetItemAt(location).HasValue &&
                       level.GetItemAt(location).AttackBonus > item.AttackBonus &&
                       level.GetItemAt(location).DefenceBonus > item.DefenceBonus;
            }

            int GetCost(Location location)
            {
                if (level.Field[location] == CellType.Wall ||
                    level.Field[location] == CellType.Trap ||
                    level.GetMonsterAt(location).HasValue)
                    return 100;

                if (level.GetItemAt(location).HasValue)
                {
                    level.Player.TryGetEquippedItem(out var item);

                    if (level.GetItemAt(location).AttackBonus > item.AttackBonus &&
                        level.GetItemAt(location).DefenceBonus > item.DefenceBonus)
                        return 0;

                    return 100;
                }

                if (level.Field[location] == CellType.Exit) return 0;

                if (level.Field[location] == CellType.Empty || level.Field[location] == CellType.PlayerStart)
                    return 10 - GetNeighbours(location, NeighbourType.Eight)
                               .Count(x => level.Field[x] == CellType.Trap ||
                                           level.Field[x] == CellType.Wall);

                if (level.Field[location] == CellType.Hidden) return 10;

                throw new ArgumentOutOfRangeException(nameof(location));
            }
        }

        private Location GetBestStepToExit(LevelView level)
        {
            return FindByDijkstra(level, IsToConsider, GetCost);

            bool IsToConsider(Location location)
            {
                return location == _exit && _exit != default(Location) || GetNeighbours(location, NeighbourType.Four)
                           .Any(y => level.Field[y] == CellType.Hidden && _hiddenYet.Contains(y));
            }

            int GetCost(Location location)
            {
                if (level.Field[location] == CellType.Wall ||
                    level.Field[location] == CellType.Trap ||
                    level.GetMonsterAt(location).HasValue ||
                    level.GetItemAt(location).HasValue)
                    return 100;

                if (level.Field[location] == CellType.Exit) return 0;

                if (level.Field[location] == CellType.Empty || level.Field[location] == CellType.PlayerStart)
                    return _exit != default(Location)
                        ? 1
                        : 10 - GetNeighbours(location, NeighbourType.Eight)
                              .Count(x => level.Field[x] == CellType.Trap ||
                                          level.Field[x] == CellType.Wall);

                if (level.Field[location] == CellType.Hidden) return 10;

                throw new ArgumentOutOfRangeException(nameof(location));
            }
        }

        private static IEnumerable<Location> GetNeighbours(Location location, NeighbourType neighbourType)
        {
            switch (neighbourType)
            {
                case NeighbourType.Four:
                    return new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }.Select(tuple =>
                          new Location(location.X + tuple.Item1, location.Y + tuple.Item2));
                case NeighbourType.Eight:
                    return new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, 1), (-1, -1), (1, -1) }.Select(tuple =>
                          new Location(location.X + tuple.Item1, location.Y + tuple.Item2));
                default: throw new ArgumentOutOfRangeException(nameof(neighbourType));
            }
        }

        private Location FindByDijkstra(LevelView level, Predicate<Location> isToConsider, Func<Location, int> getCost)
        {
            var map = _emptiesOnceWasVisible.ToDictionary(x => x,
                y => (Location: default(Location), Score: int.MaxValue));

            map[level.Player.Location] = (default(Location), 0);

            var unmarked = new HashSet<Location>(_emptiesOnceWasVisible);

            var consideredLocations =
                new HashSet<Location>(_emptiesOnceWasVisible.Where(x => isToConsider(x)).ToArray());

            if (!consideredLocations.Any()) return default(Location);

            while (unmarked.Any())
            {
                var minimalWeight = map.Where(x => unmarked.Contains(x.Key))
                    .Aggregate((result, current) => result.Value.Score < current.Value.Score ? result : current);

                if (minimalWeight.Value.Score == int.MaxValue || consideredLocations.Contains(minimalWeight.Key)) break;

                unmarked.Remove(minimalWeight.Key);

                foreach (var neighbour in GetNeighbours(minimalWeight.Key, NeighbourType.Four)
                    .Where(x => _emptiesOnceWasVisible.Contains(x)))
                {
                    var length = getCost(neighbour);

                    if (map[minimalWeight.Key].Score + length < map[neighbour].Score)
                        map[neighbour] = (minimalWeight.Key, map[minimalWeight.Key].Score + length);
                }
            }

            var toLocation = consideredLocations.Aggregate((result, current) =>
                map[result].Score < map[current].Score ? result : current);

            if (map[toLocation].Location == level.Player.Location)
                return toLocation;

            var pathLocation = map[toLocation].Location;

            if (pathLocation == default(Location))
                return default(Location);

            while (map[pathLocation].Location != level.Player.Location)
                pathLocation = map[pathLocation].Location;

            return pathLocation;
        }

        private enum NeighbourType
        {
            Four,
            Eight
        }
    }
}
