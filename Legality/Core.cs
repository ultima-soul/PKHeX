﻿using System.Collections.Generic;
using System.Linq;

namespace PKHeX
{
    public static partial class Legal
    {
        internal static WC6[] WC6DB;
        // PKHeX master personal.dat
        internal static readonly PersonalInfo[] PersonalAO = PersonalInfo.getPersonalArray(Properties.Resources.personal_ao, PersonalInfo.SizeAO);

        private static readonly PersonalInfo[] PersonalXY = PersonalInfo.getPersonalArray(Properties.Resources.personal_xy, PersonalInfo.SizeXY);
        private static readonly EggMoves[] EggMoveXY = EggMoves.getArray(Util.unpackMini(Properties.Resources.eggmove_xy, "xy"));
        private static readonly Learnset[] LevelUpXY = Learnset.getArray(Util.unpackMini(Properties.Resources.lvlmove_xy, "xy"));
        private static readonly EggMoves[] EggMoveAO = EggMoves.getArray(Util.unpackMini(Properties.Resources.eggmove_ao, "ao"));
        private static readonly Learnset[] LevelUpAO = Learnset.getArray(Util.unpackMini(Properties.Resources.lvlmove_ao, "ao"));
        private static readonly Evolutions[] Evolves = Evolutions.getArray(Util.unpackMini(Properties.Resources.evos_ao, "ao"));
        private static readonly EncounterArea[] SlotsA = EncounterArea.getArray(Util.unpackMini(Properties.Resources.encounter_a, "ao"));
        private static readonly EncounterArea[] SlotsO = EncounterArea.getArray(Util.unpackMini(Properties.Resources.encounter_o, "ao"));
        private static readonly EncounterArea[] SlotsX = EncounterArea.getArray(Util.unpackMini(Properties.Resources.encounter_x, "xy"));
        private static readonly EncounterArea[] SlotsY = EncounterArea.getArray(Util.unpackMini(Properties.Resources.encounter_y, "xy"));
        private static readonly EncounterArea[] DexNavA = getDexNavSlots(SlotsA);
        private static readonly EncounterArea[] DexNavO = getDexNavSlots(SlotsO);
        private static EncounterArea[] getDexNavSlots(EncounterArea[] GameSlots)
        {
            EncounterArea[] eA = new EncounterArea[GameSlots.Length];
            for (int i = 0; i < eA.Length; i++)
            {
                eA[i] = GameSlots[i];
                eA[i].Slots = eA[i].Slots.Take(20).Concat(eA[i].Slots.Skip(25)).ToArray(); // Skip 5 Rock Smash slots.
            }
            return eA;
        }

        internal static bool getDexNavValid(PK6 pk6)
        {
            bool alpha = pk6.Version == 26;
            if (!alpha && pk6.Version != 27)
                return false;
            IEnumerable<EncounterArea> locs = (alpha ? DexNavA : DexNavO).Where(l => l.Location == pk6.Met_Location);
            return locs.Select(loc => getValidEncounterSlots(pk6, loc, DexNav: true)).Any(slots => slots.Any());
        }
        internal static IEnumerable<int> getValidMoves(PK6 pk6)
        {
            List<int> r = new List<int> {0};
            
            r.AddRange(getLVLMoves(pk6.Species, pk6.CurrentLevel));
            r.AddRange(getTutorMoves(pk6.Species));
            r.AddRange(getMachineMoves(pk6.Species));
            IEnumerable<DexLevel> vs = getValidPreEvolutions(pk6);
            foreach (DexLevel evo in vs)
            {
                r.AddRange(getLVLMoves(evo.Species, evo.Level));
                r.AddRange(getTutorMoves(evo.Species));
                r.AddRange(getMachineMoves(evo.Species));
            }
            return r.Distinct().ToArray();
        }
        internal static IEnumerable<int> getValidRelearn(PK6 pk6, int skipOption)
        {
            List<int> r = new List<int> { 0 };
            int species = getBaseSpecies(pk6, skipOption);
            r.AddRange(getLVLMoves(species, 1));
            r.AddRange(getEggMoves(species));
            r.AddRange(getLVLMoves(species, 100));
            return r.Distinct();
        }
        internal static int[] getLinkMoves(PK6 pk6)
        {
            if (pk6.Species == 251 && pk6.Met_Level == 10) // Celebi
                return new[] {610, 0, 0, 0};
            if (pk6.Species == 154 && pk6.Met_Level == 50 && pk6.AbilityNumber == 4) // Meganium Hidden
                return new[] {0, 0, 0, 0};
            if (pk6.Species == 157 && pk6.Met_Level == 50 && pk6.AbilityNumber == 4) // Typhlosion Hidden
                return new[] {0, 0, 0, 0};
            if (pk6.Species == 160 && pk6.Met_Level == 50 && pk6.AbilityNumber == 4) // Feraligatr Hidden
                return new[] {0, 0, 0, 0};
            if (pk6.Species == 377 && pk6.Met_Level == 50 && pk6.AbilityNumber == 4) // Regirock Hidden
                return new[] {153, 8, 444, 359};
            if (pk6.Species == 378 && pk6.Met_Level == 50 && pk6.AbilityNumber == 4) // Regice Hidden
                return new[] {85, 133, 58, 258};
            if (pk6.Species == 379 && pk6.Met_Level == 50 && pk6.AbilityNumber == 4) // Registeel Hidden
                return new[] {442, 157, 356, 334};
            return new int[0];
        }
        internal static IEnumerable<WC6> getValidWC6s(PK6 pk6)
        {
            IEnumerable<DexLevel> vs = getValidPreEvolutions(pk6);
            if (pk6.Egg_Location > 0) // Was Egg, can't search TID/SID/OT
                return WC6DB.Where(wc6 => vs.Any(dl => dl.Species == wc6.Species));

            // Not Egg
            return WC6DB.Where(wc6 =>
                wc6.CardID == pk6.SID &&
                wc6.TID == pk6.TID &&
                vs.Any(dl => dl.Species == wc6.Species) &&
                wc6.OT == pk6.OT_Name);
        }
        internal static bool getWildEncounterValid(PK6 pk6)
        {
            var areas = getEncounterAreas(pk6);
            bool dexNav = pk6.RelearnMove1 != 0;
            return areas.Any(a => getValidEncounterSlots(pk6, a, dexNav).Any());
        }
        internal static int getFriendSafariValid(PK6 pk6)
        {
            int vers = pk6.Version;
            if (vers != 24 && vers != 25)
                return -1;
            IEnumerable<DexLevel> vs = getValidPreEvolutions(pk6);
            foreach (DexLevel d in vs.Where(d => FriendSafari.Contains(d.Species) && d.Level >= 30))
                return d.Species;

            return -1;
        }
        internal static IEnumerable<int> getBaseEggMoves(PK6 pk6, int skipOption, int gameSource)
        {
            int species = getBaseSpecies(pk6, skipOption);
            if (gameSource == -1)
            {
                if (pk6.Version == 24 || pk6.Version == 25)
                    return LevelUpXY[species].getMoves(1);
                // if (pk6.Version == 26 || pk6.Version == 27)
                    return LevelUpAO[species].getMoves(1);
            }
            if (gameSource == 0) // XY
                return LevelUpXY[species].getMoves(1);
            // if (gameSource == 1) // ORAS
                return LevelUpAO[species].getMoves(1);
        }
        internal static EncounterStatic getStaticEncounter(PK6 pk6)
        {
            // Get possible encounters
            IEnumerable<EncounterStatic> poss = getStaticEncounters(pk6);
            // Back Check against pk6
            foreach (EncounterStatic e in poss)
            {
                if (e.Nature != Nature.Random && pk6.Nature != (int)e.Nature)
                    continue;
                if (e.EggLocation != pk6.Egg_Location)
                    continue;
                if (e.Location != 0 && e.Location != pk6.Met_Location)
                    continue;
                if (e.Ability == 4 && pk6.AbilityNumber != 4) // Hidden can't be changed by Ability Capsule
                    continue;
                if (e.Ability != 4 && pk6.AbilityNumber == 4) // Shouldn't be Hidden
                    continue;
                if (e.Gender != -1 && e.Gender != pk6.Gender)
                    continue;
                if (e.Level != pk6.Met_Level)
                    continue;
                if (e.Shiny != null && e.Shiny != pk6.IsShiny)
                    continue;
                if (e.Gift && pk6.Ball != 4) // PokéBall
                    continue;

                // Passes all checks, valid encounter
                return e;
            }
            return null;
        }

        private static int getBaseSpecies(PK6 pk6, int skipOption)
        {
            DexLevel[] evos = Evolves[pk6.Species].Evos;
            switch (skipOption)
            {
                case -1: return pk6.Species;
                case 1: return evos.Length <= 1 ? pk6.Species : evos[evos.Length - 2].Species;
                default: return evos.Length <= 0 ? pk6.Species : evos.Last().Species;
            }
        }
        private static IEnumerable<int> getLVLMoves(int species, int lvl)
        {
            return LevelUpXY[species].getMoves(lvl).Concat(LevelUpAO[species].getMoves(lvl));
        }
        private static IEnumerable<EncounterArea> getEncounterSlots(PK6 pk6)
        {
            switch (pk6.Version)
            {
                case 24: // X
                    return getSlots(pk6, SlotsX);
                case 25: // Y
                    return getSlots(pk6, SlotsY);
                case 26: // AS
                    return getSlots(pk6, SlotsA);
                case 27: // OR
                    return getSlots(pk6, SlotsO);
                default: return new List<EncounterArea>();
            }
        }
        private static IEnumerable<EncounterStatic> getStaticEncounters(PK6 pk6)
        {
            switch (pk6.Version)
            {
                case 24: // X
                    return getStatic(pk6, StaticX);
                case 25: // Y
                    return getStatic(pk6, StaticY);
                case 26: // AS
                    return getStatic(pk6, StaticA);
                case 27: // OR
                    return getStatic(pk6, StaticO);
                default: return new List<EncounterStatic>();
            }
        }
        private static IEnumerable<EncounterArea> getEncounterAreas(PK6 pk6)
        {
            return getEncounterSlots(pk6).Where(l => l.Location == pk6.Met_Location);
        }
        private static IEnumerable<EncounterSlot> getValidEncounterSlots(PK6 pk6, EncounterArea loc, bool DexNav)
        {
            // Get Valid levels
            IEnumerable<DexLevel> vs = getValidPreEvolutions(pk6);
            // Get slots where pokemon can exist
            IEnumerable<EncounterSlot> slots = loc.Slots.Where(slot => vs.Any(evo => evo.Species == slot.Species && evo.Level >= slot.LevelMin));

            // Filter for Met Level
            slots = DexNav
                ? slots.Where(slot => slot.LevelMin <= pk6.Met_Level && pk6.Met_Level <= slot.LevelMin + 10) // DexNav Boost Range
                : slots.Where(slot => slot.LevelMin == pk6.Met_Level); // Non-boosted Level

            // Filter for Form Specific
            if (WildForms.Contains(pk6.Species))
                slots = slots.Where(slot => slot.Form == pk6.AltForm);
            return slots;
        }
        private static IEnumerable<EncounterArea> getSlots(PK6 pk6, IEnumerable<EncounterArea> tables)
        {
            int species = pk6.Species;
            IEnumerable<DexLevel> vs = getValidPreEvolutions(pk6);
            List<EncounterArea> slotLocations = new List<EncounterArea>();
            foreach (var loc in tables)
            {
                IEnumerable<EncounterSlot> slots = loc.Slots.Where(slot => vs.Any(evo => evo.Species == slot.Species && evo.Level >= slot.LevelMin));
                if (WildForms.Contains(species))
                    slots = slots.Where(slot => slot.Form == pk6.AltForm);

                EncounterSlot[] es = slots.ToArray();
                if (es.Length > 0)
                    slotLocations.Add(new EncounterArea { Location = loc.Location, Slots = es });
            }
            return slotLocations;
        }
        private static IEnumerable<DexLevel> getValidPreEvolutions(PK6 pk6)
        {
            var evos = Evolves[pk6.Species].Evos;
            List<DexLevel> dl = new List<DexLevel> { new DexLevel { Species = pk6.Species, Level = pk6.CurrentLevel } };
            int lvl = pk6.CurrentLevel;
            foreach (DexLevel evo in evos)
            {
                if (lvl >= pk6.Met_Level && lvl <= evo.Level)
                    dl.Add(new DexLevel { Species = evo.Species, Level = lvl });
                if (evo.Level > 0) // Level Up (from previous level)
                    lvl--;
            }
            return dl;
        }
        private static IEnumerable<EncounterStatic> getStatic(PK6 pk6, IEnumerable<EncounterStatic> table)
        {
            IEnumerable<DexLevel> dl = getValidPreEvolutions(pk6);
            return table.Where(e => dl.Any(d => d.Species == e.Species));
        }
        private static IEnumerable<int> getEggMoves(int species)
        {
            return EggMoveAO[species].Moves.Concat(EggMoveXY[species].Moves);
        }
        private static IEnumerable<int> getTutorMoves(int species)
        {
            PersonalInfo pkAO = PersonalAO[species];

            // Type Tutor
            List<int> moves = TypeTutor.Where((t, i) => pkAO.Tutors[i]).ToList();

            // Varied Tutors
            for (int i = 0; i < Tutors_AO.Length; i++)
                for (int b = 0; b < Tutors_AO[i].Length; b++)
                    if (pkAO.ORASTutors[i][b])
                        moves.Add(Tutors_AO[i][b]);

            return moves;
        }
        private static IEnumerable<int> getMachineMoves(int species)
        {
            PersonalInfo pkXY = PersonalXY[species];
            PersonalInfo pkAO = PersonalAO[species];
            List<int> moves = new List<int>();
            moves.AddRange(TMHM_XY.Where((t, i) => pkXY.TMHM[i]));
            moves.AddRange(TMHM_AO.Where((t, i) => pkAO.TMHM[i]));
            return moves;
        }

        #region Static Encounter/Gift Tables
        private static readonly EncounterStatic[] Encounter_XY =
        {
            new EncounterStatic { Species = 650, Level = 5, Location = 10, Gift = true }, // Chespin
            new EncounterStatic { Species = 653, Level = 5, Location = 10, Gift = true }, // Fennekin
            new EncounterStatic { Species = 656, Level = 5, Location = 10, Gift = true }, // Froakie

            new EncounterStatic { Species = 1, Level = 10, Location = 10, Gift = true }, // Bulbasaur
            new EncounterStatic { Species = 4, Level = 10, Location = 10, Gift = true }, // Charmander
            new EncounterStatic { Species = 7, Level = 10, Location = 10, Gift = true }, // Squirtle

            new EncounterStatic { Species = 448, Level = 32, Location = 22, Ability = 1, Nature = Nature.Hasty, Gift = true }, // Lucario
            new EncounterStatic { Species = 448, Level = 32, Location = 22, Ability = 1, Nature = Nature.Docile, Gift = true }, // Lapras
            
            new EncounterStatic { Species = 143, Level = 15, Location = 38 }, // Snorlax
            
            new EncounterStatic { Species = 716, Level = 50, Location = 138, Version = GameVersion.X, Shiny = false }, // Xerneas
            new EncounterStatic { Species = 717, Level = 50, Location = 138, Version = GameVersion.Y, Shiny = false }, // Yveltal
            new EncounterStatic { Species = 718, Level = 70, Location = 140, Shiny = false }, // Zygarde
            
            new EncounterStatic { Species = 150, Level = 70, Location = 168, Shiny = false }, // Mewtwo

            new EncounterStatic { Species = 144, Level = 70, Location = 146, Shiny = false }, // Articuno
            new EncounterStatic { Species = 145, Level = 70, Location = 146, Shiny = false }, // Zapdos
            new EncounterStatic { Species = 146, Level = 70, Location = 146, Shiny = false }, // Moltres
        };
        private static readonly EncounterStatic[] Encounter_AO =
        {
            new EncounterStatic { Species = 152, Level = 5, Location = 170, Gift = true }, // Chespin
            new EncounterStatic { Species = 155, Level = 5, Location = 170, Gift = true }, // Fennekin
            new EncounterStatic { Species = 158, Level = 5, Location = 170, Gift = true }, // Froakie
            
            new EncounterStatic { Species = 252, Level = 5, Location = 204, Gift = true }, // Chikorita
            new EncounterStatic { Species = 255, Level = 5, Location = 204, Gift = true }, // Cyndaquil
            new EncounterStatic { Species = 258, Level = 5, Location = 204, Gift = true }, // Totodile

            new EncounterStatic { Species = 387, Level = 5, Location = 204, Gift = true }, // Turtwig
            new EncounterStatic { Species = 390, Level = 5, Location = 204, Gift = true }, // Chimchar
            new EncounterStatic { Species = 393, Level = 5, Location = 204, Gift = true }, // Piplup

            new EncounterStatic { Species = 495, Level = 5, Location = 204, Gift = true }, // Snivy
            new EncounterStatic { Species = 498, Level = 5, Location = 204, Gift = true }, // Tepig
            new EncounterStatic { Species = 501, Level = 5, Location = 204, Gift = true }, // Oshawott

            new EncounterStatic { Species = 25, Level = 20, Location = 186, Gender = 1, Ability = 4, Gift = true }, // Pikachu
            new EncounterStatic { Species = 360, Level = 1, EggLocation = 176, Gift = true }, // Wynaut
            new EncounterStatic { Species = 175, Level = 1, EggLocation = 176, Ability = 1, Gift = true }, // Togepi
            new EncounterStatic { Species = 360, Level = 1, Location = 196, Gift = true }, // Beldum

            new EncounterStatic { Species = 319, Level = 40, Location = 318, Gift = true }, // Sharpedo
            new EncounterStatic { Species = 323, Level = 40, Location = 318, Gift = true }, // Camerupt

            new EncounterStatic { Species = 382, Level = 45, Location = 296, Version = GameVersion.AS, Shiny = false }, // Kyogre
            new EncounterStatic { Species = 383, Level = 45, Location = 296, Version = GameVersion.OR, Shiny = false }, // Groudon
            new EncounterStatic { Species = 384, Level = 70, Location = 316, Shiny = false }, // Rayquaza
            new EncounterStatic { Species = 386, Level = 80, Location = 316, Shiny = false }, // Deoxys

            new EncounterStatic { Species = 377, Level = 40, Location = 278 }, // Regirock
            new EncounterStatic { Species = 378, Level = 40, Location = 306 }, // Regice
            new EncounterStatic { Species = 379, Level = 40, Location = 308 }, // Registeel
            new EncounterStatic { Species = 486, Level = 50, Location = 306 }, // Regigigas
            
            new EncounterStatic { Species = 249, Level = 50, Location = 304, Version = GameVersion.AS }, // Lugia
            new EncounterStatic { Species = 250, Level = 50, Location = 306, Version = GameVersion.OR }, // Ho-oh

            new EncounterStatic { Species = 383, Level = 50, Location = 348, Version = GameVersion.AS }, // Dialga
            new EncounterStatic { Species = 384, Level = 50, Location = 348, Version = GameVersion.OR }, // Palia

            new EncounterStatic { Species = 644, Level = 50, Location = 340, Version = GameVersion.AS }, // Zekrom
            new EncounterStatic { Species = 643, Level = 50, Location = 340, Version = GameVersion.OR }, // Reshiram

            new EncounterStatic { Species = 642, Level = 50, Location = 348, Version = GameVersion.AS }, // Thundurus
            new EncounterStatic { Species = 641, Level = 50, Location = 348, Version = GameVersion.OR }, // Tornadus

            new EncounterStatic { Species = 485, Level = 50, Location = 312 }, // Heatran
            new EncounterStatic { Species = 487, Level = 50, Location = 348 }, // Giratina
            new EncounterStatic { Species = 488, Level = 50, Location = 344 }, // Cresselia
            new EncounterStatic { Species = 645, Level = 50, Location = 348 }, // Landorus
            new EncounterStatic { Species = 646, Level = 50, Location = 342 }, // Kyurem
            
            new EncounterStatic { Species = 243, Level = 50, Location = 334 }, // Raikou
            new EncounterStatic { Species = 244, Level = 50, Location = 334 }, // Entei
            new EncounterStatic { Species = 245, Level = 50, Location = 334 }, // Suicune

            new EncounterStatic { Species = 480, Level = 50, Location = 338 }, // Uxie
            new EncounterStatic { Species = 481, Level = 50, Location = 338 }, // Mesprit
            new EncounterStatic { Species = 482, Level = 50, Location = 338 }, // Azelf

            new EncounterStatic { Species = 638, Level = 50, Location = 336 }, // Cobalion
            new EncounterStatic { Species = 639, Level = 50, Location = 336 }, // Terrakion
            new EncounterStatic { Species = 640, Level = 50, Location = 336 }, // Virizion
            
            new EncounterStatic { Species = 352, Level = 30, Location = 240 }, // Kecleon @ Route 119
            new EncounterStatic { Species = 352, Level = 30, Location = 242 }, // Kecleon @ Route 120
            new EncounterStatic { Species = 352, Level = 40, Location = 176 }, // Kecleon @ Lavaridge
            new EncounterStatic { Species = 352, Level = 45, Location = 196 }, // Kecleon @ Mossdeep City
            
            new EncounterStatic { Species = 380, Level = 30, Location = 320, Version = GameVersion.AS, Gift = true }, // Latias
            new EncounterStatic { Species = 381, Level = 30, Location = 320, Version = GameVersion.OR, Gift = true }, // Latios

            new EncounterStatic { Species = 381, Level = 30, Location = 320, Version = GameVersion.AS }, // Latios
            new EncounterStatic { Species = 380, Level = 30, Location = 320, Version = GameVersion.OR }, // Latias
            
            new EncounterStatic { Species = 101, Level = 40, Location = 292, Version = GameVersion.AS }, // Electrode
            new EncounterStatic { Species = 101, Level = 40, Location = 314, Version = GameVersion.OR }, // Electrode
            
            new EncounterStatic { Species = 100, Level = 20, Location = 302 }, // Voltorb @ Route 119
            new EncounterStatic { Species = 442, Level = 50, Location = 304 }, // Spiritomb @ Route 120
        };
        #endregion
        private static readonly EncounterStatic[] StaticX = getSpecial(GameVersion.X);
        private static readonly EncounterStatic[] StaticY = getSpecial(GameVersion.Y);
        private static readonly EncounterStatic[] StaticA = getSpecial(GameVersion.AS);
        private static readonly EncounterStatic[] StaticO = getSpecial(GameVersion.OR);
        private static EncounterStatic[] getSpecial(GameVersion Game)
        {
            if (Game == GameVersion.X || Game == GameVersion.Y)
                return Encounter_XY.Where(s => s.Version == GameVersion.Any || s.Version == Game).ToArray();
            // else if (Game == GameVersion.AS || Game == GameVersion.OR)
            return Encounter_AO.Where(s => s.Version == GameVersion.Any || s.Version == Game).ToArray();
        }
    }
}
