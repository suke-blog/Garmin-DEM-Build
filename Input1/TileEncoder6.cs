﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

/*
CS0659: "Klasse" überschreibt Object.Equals(object o), aber nicht Object.GetHashCode() ('class' overrides Object.Equals(object o) but does not override Object.GetHashCode())
CS0660: "Klasse" definiert den Operator == oder !=, aber überschreibt Object.Equals(object o) nicht. ('class' defines operator == or operator != but does not override Object.Equals(object o))
CS0661: "Klasse" definiert den Operator == oder !=, aber überschreibt Object.GetHashCode() nicht. ('class' defines operator == or operator != but does not override Object.GetHashCode())
#pragma warning disable 659, 661
*/

namespace Encoder {

   /// <summary>
   /// Encoder für eine einzelne DEM-Kachel
   /// </summary>
   public class TileEncoder {

      #region Hilfsklassen

      /// <summary>
      /// zum einfacheren Umgang mit Positionen
      /// </summary>
      class Position {
         /// <summary>
         /// Matrix-Breite
         /// </summary>
         public int Width { get; private set; }
         /// <summary>
         /// Matrix-Höhe
         /// </summary>
         public int Height { get; private set; }
         /// <summary>
         /// Position waagerecht
         /// </summary>
         public int X { get; private set; }
         /// <summary>
         /// Position senkrecht
         /// </summary>
         public int Y { get; private set; }
         /// <summary>
         /// liefert oder setzt den fortlaufenden Index der Position (0 .. (<see cref="Width"/> * <see cref="Height"/> - 1))
         /// </summary>
         public int Idx {
            get {
               return Y * Width + X;
            }
            set {
               if (value < Width * Height) {
                  PositionError = false;
                  Set(value % Width, value / Height);
               } else {
                  PositionError = true;
                  Set(Width - 1, Height - 1);
               }
            }
         }
         /// <summary>
         /// liefert true, wenn die letzte Positionierung eine Bereichsüberschreitung ergab
         /// </summary>
         public bool PositionError { get; private set; }


         /// <summary>
         /// erzeugt eine Position
         /// </summary>
         /// <param name="width">Breite</param>
         /// <param name="height">Höhe</param>
         /// <param name="x">Position waagerecht</param>
         /// <param name="y">Position senkrecht</param>
         public Position(int width, int height, int x = 0, int y = 0) {
            Width = width;
            Height = height;
            Set(x, y);
         }

         /// <summary>
         /// erzeugt eine Position als Differenz zu einer existierenden Position
         /// </summary>
         /// <param name="pos">existierende Position</param>
         /// <param name="deltax">waagerechte Differenz</param>
         /// <param name="deltay">senkrechte Differenz</param>
         public Position(Position pos, int deltax = 0, int deltay = 0) {
            X = pos.X + deltax;
            Y = pos.Y + deltay;
            Width = pos.Width;
            Height = pos.Height;
         }

         public bool Equals(Position pos) {
            if ((object)pos == null) // NICHT "p == null" usw. --> führt zur Endlosschleife
               return false;
            return (X == pos.X) && (Y == pos.Y);
         }

         public override bool Equals(object obj) {
            if (obj == null)
               return false;

            // If parameter cannot be cast to Point return false.
            Position p = obj as Position;
            if (p == null)
               return false;

            return (X == p.X) && (Y == p.Y);
         }

         public override int GetHashCode() {
            return base.GetHashCode();
         }


         public static bool operator ==(Position a, Position b) {
            return a.Equals(b);
         }

         public static bool operator !=(Position a, Position b) {
            return !a.Equals(b);
         }

         public static bool operator <=(Position a, Position b) {
            return a.Idx <= b.Idx;
         }

         public static bool operator >=(Position a, Position b) {
            return a.Idx >= b.Idx;
         }

         public static bool operator <(Position a, Position b) {
            return a.Idx < b.Idx;
         }

         public static bool operator >(Position a, Position b) {
            return a.Idx > b.Idx;
         }


         /// <summary>
         /// setzt die Position (automatisch begrenzt durch den "Rand")
         /// </summary>
         /// <param name="x"></param>
         /// <param name="y"></param>
         /// <returns>false, falls der Bereich überschritten würde</returns>
         public bool Set(int x, int y) {
            PositionError = false;
            if (x < 0 || x >= Width) {
               PositionError = true;
               if (x < 0)
                  x = 0;
               else
                  x = Width - 1;
            }
            if (y < 0 || y >= Height) {
               PositionError = true;
               if (y < 0)
                  y = 0;
               else
                  y = Height - 1;
            }
            X = x;
            Y = y;
            return !PositionError;
         }

         /// <summary>
         /// Änderung der Position (automatisch begrenzt durch den "Rand")
         /// </summary>
         /// <param name="deltax"></param>
         /// <param name="deltay"></param>
         /// <returns>false, falls der Bereich überschritten würde</returns>
         public bool Move(int deltax, int deltay) {
            PositionError = false;
            X += deltax;
            if (X < 0) {
               X = 0;
               PositionError = true;
            } else if (X >= Width) {
               X = Width - 1;
               PositionError = true;
            }
            if (Y < 0) {
               Y = 0;
               PositionError = true;
            } else if (Y >= Height) {
               Y = Height - 1;
               PositionError = true;
            }
            return !PositionError;
         }

         /// <summary>
         /// rückt die Position um die entsprechende Anzahl Schritte, ev. auch mit Zeilenwechsel, weiter
         /// </summary>
         /// <param name="count">Anzahl der Schritte</param>
         /// <returns>false, falls der Bereich überschritten würde</returns>
         public bool MoveForward(int count = 1) {
            PositionError = false;
            if (count == 1) {
               if (++X >= Width)
                  if (Y < Height - 1) {
                     Y++;
                     X = 0;
                  } else {
                     X = Width - 1;
                     PositionError = true;
                  }
            } else {
               Idx = Idx + count;
               if (Y > Height - 1 ||
                   X > Width - 1) {
                  Y = Height - 1;
                  X = Width - 1;
                  PositionError = true;
               }
            }
            return !PositionError;
         }

         /// <summary>
         /// rückt die Position 1 Schritt, ev. auch mit Zeilenwechsel, zurück
         /// </summary>
         /// <returns>false, falls der Bereich überschritten würde</returns>
         public bool MoveBackward() {
            PositionError = false;
            if (--X < 0)
               if (Y > 0) {
                  Y--;
                  X = Width - 1;
               } else {
                  X = 0;
                  PositionError = true;
               }
            return !PositionError;
         }

         /// <summary>
         /// ändert die vertikale Position um die entsprechende Anzahl Schritte (automatisch begrenzt durch den "Rand")
         /// </summary>
         /// <param name="count">Anzahl der Schritte</param>
         /// <returns>false, falls der Bereich überschritten würde</returns>
         public bool MoveVertical(int count) {
            PositionError = false;
            if (count != 0)
               Y += count;
            if (Y < 0) {
               Y = 0;
               PositionError = true;
            } else if (Y >= Height) {
               Y = Height - 1;
               PositionError = true;
            }
            return !PositionError;
         }

         public override string ToString() {
            return string.Format("X={0}, Y={1}, Width={2}, Height={3}, PositionError={4}", X, Y, Width, Height, PositionError);
         }

      }


      /* Die HeightUnit wird nicht für alle Datenwerte auf gleiche Art bestimmt. Bestimmte Datenwerte werden jeweils in eigenen Gruppen zusammengefasst deren HeightUnit-Berechnung auch
       * unterschiedliche sein kann.
       * I.A. dürfte es sinnvoll sein, mit der max. Höhe der Kachel zu init.. Die Summe der Absolutwerte wird mit 0 init., die Tabellenspalte zum "Ablesen" des Wertes mit 0.
       * Bei jedem Hinzufügen eines Wertes wird die Summe um den Absolutwert des Wertes erhöht und die Tabellenspalte inkrementiert.
       * Danach folgt eine klassenspeziefische Korrektur beider Werte.
       * Anschließend wird auf der Basis dieser beiden Werte die HeightUnit bestimmt.
       */

      /// <summary>
      /// Ein Objekt dieser Klasse kann nur mit einer der statischen Funktionen erzeugt werden.
      /// <para>Ein Höhenelement beschreibt im einfachsten Fall die Höhe an einer einzelnen Position. Es kann damit aber auch ein Plateau beschrieben werden.
      /// Für die Nachfolgehöhe eines Plateaus gelten auch spezielle Regeln.</para>
      /// </summary>
      public class HeightElement {

         /// <summary>
         /// Art der Codierung
         /// </summary>
         public enum EncodeMode {
            /// <summary>
            /// noch nicht definiert
            /// </summary>
            notdefined,
            /// <summary>
            /// hybride Codierung
            /// </summary>
            Hybrid,
            /// <summary>
            /// Längencodierung Variante 0
            /// </summary>
            Length0,
            /// <summary>
            /// Längencodierung Variante 1
            /// </summary>
            Length1,
            /// <summary>
            /// Spezialcodierung für Plateau
            /// </summary>
            Plateau,
            /// <summary>
            /// Codierung im "festen" Binärformat (für große Zahlen)
            /// </summary>
            BigBinary,
            /// <summary>
            /// Codierung im "festen" Binärformat (für große Zahlen an Stelle der <see cref="Length1"/>-Codierung)
            /// </summary>
            BigBinaryL1,
         }

         /// <summary>
         /// Typ des Höhen-Elements
         /// </summary>
         public enum Typ {
            /// <summary>
            /// normaler Höhenwert
            /// </summary>
            Value,
            /// <summary>
            /// ein Plateau
            /// </summary>
            Plateau,
            /// <summary>
            /// Wert hinter einem Plateau
            /// </summary>
            PlateauFollower
         }

         /// <summary>
         /// Typ des Elements
         /// </summary>
         public Typ ElementTyp { get; private set; }
         /// <summary>
         /// Datenwert
         /// </summary>
         public int Data { get; private set; }
         /// <summary>
         /// Codierart
         /// </summary>
         public EncodeMode Encoding { get; private set; }
         /// <summary>
         /// HUnit, gültig nur bei Hybridcodierung
         /// </summary>
         public int HUnit { get; private set; }
         /// <summary>
         /// Bit-Liste der Codierung
         /// </summary>
         public List<byte> Bits { get; private set; }
         /// <summary>
         /// Element bezieht sich auf diese Zeile
         /// </summary>
         public int Line { get; private set; }
         /// <summary>
         /// Element bezieht sich auf diese Spalte
         /// </summary>
         public int Column { get; private set; }
         /// <summary>
         /// 
         /// </summary>
         public int PlateauFollowerDdiff { get; private set; }
         /// <summary>
         /// Tabellenpos. für die Unit des 1. 1-Bit des nächsten (!) Plateaus
         /// </summary>
         public int PlateauTabPointer4Unit { get; private set; }
         /// <summary>
         /// Tabellenpos. für die Bitanzahl des 1. 1-Bit des nächsten (!) Plateaus
         /// </summary>
         public int PlateauTabPointer4Bits { get; private set; }

         /// <summary>
         /// Bitliste (nach dem Encodieren)
         /// </summary>
         List<char> BitUnits;

         /// <summary>
         /// Hilfstabelle für die Codierung
         /// </summary>
         static List<int> PlateauBitUnitTable;

         /// <summary>
         /// Hilfstabelle für die Codierung
         /// </summary>
         static List<int> PlateauBitTable;

         List<int> Lines4Unit8;
         int lastplateauendline;


         static HeightElement() {
            BuildPlateauTables();
         }

         /// <summary>
         /// Dieser Konstruktor kann nur klassenintern aufgerufen werden. Das erfolgt über die statischen CreateHeightElement-Funktionen.
         /// </summary>
         /// <param name="typ"></param>
         /// <param name="data"></param>
         /// <param name="encoding"></param>
         /// <param name="column"></param>
         /// <param name="line"></param>
         /// <param name="hunit"></param>
         /// <param name="maxheigth"></param>
         /// <param name="linelength"></param>
         /// <param name="lastplateau"></param>
         /// <param name="plateaufollowerddiff"></param>
         protected HeightElement(Typ typ,
                                 int data,
                                 EncodeMode encoding,
                                 int column,
                                 int line,
                                 int hunit,
                                 int maxheigth,
                                 int linelength,
                                 HeightElement lastplateau,
                                 int plateaufollowerddiff) {
            Bits = new List<byte>();

            Column = column;
            Line = line;
            ElementTyp = typ;
            Data = data;
            Encoding = encoding;
            HUnit = hunit;
            PlateauFollowerDdiff = plateaufollowerddiff;
            if (lastplateau != null) {
               PlateauTabPointer4Unit = lastplateau.PlateauTabPointer4Unit;
               PlateauTabPointer4Bits = lastplateau.PlateauTabPointer4Bits;
               Lines4Unit8 = new List<int>(lastplateau.Lines4Unit8);
               lastplateauendline = lastplateau.lastplateauendline;
            } else {
               PlateauTabPointer4Unit = 0;
               PlateauTabPointer4Bits = 0;
               Lines4Unit8 = new List<int>();
               lastplateauendline = 0;
            }

            switch (typ) {
               case Typ.Value:
               case Typ.PlateauFollower:
                  switch (Encoding) {
                     case EncodeMode.Hybrid:
                        EncodeHybrid(data, maxheigth, hunit);
                        break;

                     case EncodeMode.Length0:
                        EncodeLength0(data);
                        break;

                     case EncodeMode.Length1:
                        EncodeLength1(data);
                        break;

                     case EncodeMode.BigBinary:
                     case EncodeMode.BigBinaryL1:
                        EncodeBigBin(data, maxheigth, typ == Typ.PlateauFollower);
                        break;

                     default:
                        throw new Exception(string.Format("Falscher Codiertyp: {0}.", Encoding));
                  }
                  break;

               case Typ.Plateau:
                  EncodePlateau(data, column, line, linelength);
                  break;
            }
         }

         #region CreateHeightElement-Funktionen zum Erzeugen eines Objektes

         /// <summary>
         /// erzeugt ein <see cref="HeightElement"/> für einen "normalen" Wert mit Hybridcodierung
         /// </summary>
         /// <param name="data">Datenwert</param>
         /// <param name="maxheigth">max. Höhe der Kachel (laut Def.)</param>
         /// <param name="hunit">Heightunit für den Wert</param>
         /// <param name="column">akt. Spalte</param>
         /// <param name="line">akt. Zeile</param>
         /// <returns></returns>
         static public HeightElement CreateHeightElement_ValueH(int data, int maxheigth, int hunit, int column, int line) {
            return new HeightElement(HeightElement.Typ.Value, data, HeightElement.EncodeMode.Hybrid, column, line, hunit, maxheigth, int.MinValue, null, int.MinValue);
         }

         /// <summary>
         /// erzeugt ein <see cref="HeightElement"/> für den Plateaunachfolger (analog einem ganz normalen Wert)
         /// </summary>
         /// <param name="data">Datenwert für den Plateaunachfolger</param>
         /// <param name="maxheigth">max. Höhe der Kachel (laut Def.)</param>
         /// <param name="hunit">Heightunit für den Plateaunachfolger</param>
         /// <param name="ddiff">Diagonaldiff. für den Plateaunachfolger</param>
         /// <param name="column">akt. Spalte</param>
         /// <param name="line">akt. Zeile</param>
         /// <returns></returns>
         static public HeightElement CreateHeightElement_PlateauFollowerH(int data, int maxheigth, int hunit, int ddiff, int column, int line) {
            return new HeightElement(HeightElement.Typ.PlateauFollower, data, HeightElement.EncodeMode.Hybrid, column, line, hunit, maxheigth, int.MinValue, null, ddiff);
         }

         /// <summary>
         /// erzeugt ein <see cref="HeightElement"/> für einen "normalen" Wert mit Längencodierung
         /// </summary>
         /// <param name="data">Datenwert</param>
         /// <param name="l0">Längencodierung Variante 0 (true) oder 1 (false)</param>
         /// <param name="column">akt. Spalte</param>
         /// <param name="line">akt. Zeile</param>
         /// <returns></returns>
         static public HeightElement CreateHeightElement_ValueL(int data, bool l0, int column, int line) {
            return new HeightElement(HeightElement.Typ.Value, data, l0 ? HeightElement.EncodeMode.Length0 : HeightElement.EncodeMode.Length1, column, line, int.MinValue, int.MinValue, int.MinValue, null, int.MinValue);
         }

         /// <summary>
         /// erzeugt ein <see cref="HeightElement"/> für den Plateaunachfolger in Längencodierung
         /// </summary>
         /// <param name="data">Datenwert</param>
         /// <param name="l0">Längencodierung Variante 0 (true) oder 1 (false)</param>
         /// <param name="ddiff">Diagonaldiff. für den Plateaunachfolger</param>
         /// <param name="column">akt. Spalte</param>
         /// <param name="line">akt. Zeile</param>
         /// <returns></returns>
         static public HeightElement CreateHeightElement_PlateauFollowerL(int data, bool l0, int ddiff, int column, int line) {
            return new HeightElement(HeightElement.Typ.PlateauFollower, data, l0 ? HeightElement.EncodeMode.Length0 : HeightElement.EncodeMode.Length1, column, line, int.MinValue, int.MinValue, int.MinValue, null, ddiff);
         }

         /// <summary>
         /// erzeugt ein <see cref="HeightElement"/> für einen "normalen", aber großen Wert mit Hybridcodierung
         /// </summary>
         /// <param name="data">Datenwert</param>
         /// <param name="maxheigth">max. Höhe der Kachel (laut Def.)</param>
         /// <param name="column">akt. Spalte</param>
         /// <param name="line">akt. Zeile</param>
         /// <returns></returns>
         static public HeightElement CreateHeightElement_BigValue(int data, int maxheigth, int column, int line) {
            return new HeightElement(HeightElement.Typ.Value, data, HeightElement.EncodeMode.BigBinary, column, line, int.MinValue, maxheigth, int.MinValue, null, int.MinValue);
         }

         /// <summary>
         /// erzeugt ein <see cref="HeightElement"/> für einen "normalen", aber großen Wert mit Hybridcodierung (an Stelle von <see cref="EncodeMode.Length1"/>)
         /// </summary>
         /// <param name="data">Datenwert</param>
         /// <param name="maxheigth">max. Höhe der Kachel (laut Def.)</param>
         /// <param name="column">akt. Spalte</param>
         /// <param name="line">akt. Zeile</param>
         /// <returns></returns>
         static public HeightElement CreateHeightElement_BigValueL1(int data, int maxheigth, int column, int line) {
            return new HeightElement(HeightElement.Typ.Value, data, HeightElement.EncodeMode.BigBinaryL1, column, line, int.MinValue, maxheigth, int.MinValue, null, int.MinValue);
         }

         /// <summary>
         /// erzeugt ein <see cref="HeightElement"/> für einen "normalen", aber großen Wert mit Hybridcodierung
         /// </summary>
         /// <param name="data">Datenwert</param>
         /// <param name="maxheigth">max. Höhe der Kachel (laut Def.)</param>
         /// <param name="ddiff">Diagonaldiff. für den Plateaunachfolger</param>
         /// <param name="column">akt. Spalte</param>
         /// <param name="line">akt. Zeile</param>
         /// <returns></returns>
         static public HeightElement CreateHeightElement_PlateauFollowerBigValue(int data, int maxheigth, int ddiff, int column, int line) {
            return new HeightElement(HeightElement.Typ.PlateauFollower, data, HeightElement.EncodeMode.BigBinary, column, line, int.MinValue, maxheigth, int.MinValue, null, ddiff);
         }

         /// <summary>
         /// erzeugt ein <see cref="HeightElement"/> für einen "normalen", aber großen Wert mit Hybridcodierung (an Stelle von <see cref="EncodeMode.Length1"/>)
         /// </summary>
         /// <param name="data">Datenwert</param>
         /// <param name="maxheigth">max. Höhe der Kachel (laut Def.)</param>
         /// <param name="ddiff">Diagonaldiff. für den Plateaunachfolger</param>
         /// <param name="column">akt. Spalte</param>
         /// <param name="line">akt. Zeile</param>
         /// <returns></returns>
         static public HeightElement CreateHeightElement_PlateauFollowerBigValueL1(int data, int maxheigth, int ddiff, int column, int line) {
            return new HeightElement(HeightElement.Typ.PlateauFollower, data, HeightElement.EncodeMode.BigBinaryL1, column, line, int.MinValue, maxheigth, int.MinValue, null, ddiff);
         }

         /// <summary>
         /// erzeugt ein <see cref="HeightElement"/> für die Plateaulänge
         /// </summary>
         /// <param name="length">Plateaulänge</param>
         /// <param name="column">akt. Spalte</param>
         /// <param name="line">akt. Zeile</param>
         /// <param name="linelength">Zeilenlänge, d.h. Kachelbreite</param>
         /// <param name="oldheightelements">Liste aller bisher ermittelten <see cref="HeightElement"/>'s</param>
         /// <returns></returns>
         static public HeightElement CreateHeightElement_Plateau(int length, int column, int line, int linelength, IList<HeightElement> oldheightelements) {
            HeightElement last = null;
            for (int i = oldheightelements.Count - 1; i >= 0; i--) // letztes Plateau-Element suchen
               if (oldheightelements[i].ElementTyp == HeightElement.Typ.Plateau) {
                  last = oldheightelements[i];
                  break;
               }
            return new HeightElement(HeightElement.Typ.Plateau, length, HeightElement.EncodeMode.Plateau, column, line, int.MinValue, int.MinValue, linelength, last, int.MinValue);
         }

         #endregion

         /// <summary>
         /// liefert die Bits als Text
         /// </summary>
         /// <returns></returns>
         public string GetBinText() {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Bits.Count; i++)
               sb.Append(Bits[i] != 0 ? "1" : "0");
            sb.Replace('0', '.');
            return sb.ToString();
         }

         /// <summary>
         /// liefert die Bits als Text
         /// </summary>
         /// <returns></returns>
         public string GetBitUnitsText() {
            return new string(BitUnits.ToArray());
         }

         /// <summary>
         /// codiert rein binär ohne Vorzeichen (MSB zuerst)
         /// </summary>
         /// <param name="data"></param>
         /// <param name="bitcount">Bitanzahl</param>
         void EncodeBinary(uint data, int bitcount) {
            if (bitcount == 0 && data == 0)
               return;
            int t = 1 << (bitcount - 1);
            if (data >= t << 1)
               throw new Exception(string.Format("Die Zahl {0} ist zu große für die binäre Codierung mit {1} Bits.", data, bitcount));
            while (t > 0) {
               Bits.Add((byte)((data & t) != 0 ? 1 : 0));
               t >>= 1;
            }
         }

         /// <summary>
         /// codiert hybrid
         /// </summary>
         /// <param name="data"></param>
         /// <param name="maxheigth"></param>
         /// <param name="hunit"></param>
         void EncodeHybrid(int data, int maxheigth, int hunit) {
            int hunitexp = HunitExponent(hunit);
            if (hunitexp < 0)
               throw new Exception(string.Format("Die Heigthunit {0} für die Codierung {1} ist kein 2er-Potenz.", hunit, EncodeMode.Hybrid));

            // v = [0,1]
            // Delta = (2*v-1) * (m * hunit + Summe[(i=0..(n-1)]{(a(i) * 2^i} + v)
            int m = 0;
            int bin = 0;

            if (data > 0) {
               // Delta = m * hunit + 1 + Summe[(i=0..(n-1)]{(a(i) * 2^i}
               bin = (data - 1) % hunit;
               m = (data - 1 - bin) / hunit;
            } else {
               // -Delta = m * hunit + Summe[(i=0..(n-1)]{(a(i) * 2^i} 
               bin = -data % hunit;
               m = (-data - bin) / hunit;
            }

            int maxm = GetMaxLengthBits(maxheigth);
            if (m <= maxm) {
               EncodeLength(m);                       // längencodierten Teil speichern
               EncodeBinary((uint)bin, hunitexp);           // binär codierten Teil speichern
               Bits.Add((byte)(data > 0 ? 1 : 0));    // Vorzeichen speichern
            } else
               throw new Exception(string.Format("Der Betrag des Wertes {0} ist für die Codierung {1} bei der Maximalhöhe {2} und mit Heigthunit {3} zu groß.",
                                                   data,
                                                   EncodeMode.Hybrid,
                                                   maxheigth,
                                                   hunit));
         }

         /// <summary>
         /// codiert eine "große" Zahl binär mit führender 0-Bitfolge
         /// </summary>
         /// <param name="data"></param>
         /// <param name="maxheigth"></param>
         /// <param name="follower">für Plateaunachfolger</param>
         void EncodeBigBin(int data, int maxheigth, bool plateaufollower) {
            if (data == 0)
               throw new Exception(string.Format("Der Wert 0 kann nicht in der Codierung {0} erzeugt werden.", EncodeMode.BigBinary));

            if (Encoding == EncodeMode.BigBinaryL1)
               data = 1 - data;

            int length0 = GetMaxLengthBits(maxheigth) + 1; // 1 Bit mehr
            if (plateaufollower)
               length0--;
            int bitcount = 0;
            if (maxheigth < 2)
               bitcount = 1;
            else if (maxheigth < 4)
               bitcount = 2;
            else if (maxheigth < 8)
               bitcount = 3;
            else if (maxheigth < 16)
               bitcount = 4;
            else if (maxheigth < 32)
               bitcount = 5;
            else if (maxheigth < 64)
               bitcount = 6;
            else if (maxheigth < 128)
               bitcount = 7;
            else if (maxheigth < 256)
               bitcount = 8;
            else if (maxheigth < 512)
               bitcount = 9;
            else if (maxheigth < 1024)
               bitcount = 10;
            else if (maxheigth < 2048)
               bitcount = 11;
            else if (maxheigth < 4096)
               bitcount = 12;
            else if (maxheigth < 8192)
               bitcount = 13;
            else if (maxheigth < 16384)
               bitcount = 14;
            else
               bitcount = 15;

            EncodeLength(length0); // 0-Bits und 1-Bit
            EncodeBinary((uint)Math.Abs(data) - 1, bitcount - 1);
            Bits.Add((byte)(data > 0 ? 0 : 1)); // Vorzeichen speichern (1 für <0)
         }

         /// <summary>
         /// Hilfsfunktion für die Längencodierung
         /// </summary>
         /// <param name="count0bits"></param>
         void EncodeLength(int count0bits) {
            for (int i = 0; i < count0bits; i++)
               Bits.Add(0);
            Bits.Add(1);
         }

         /// <summary>
         /// speichert den Wert in C0-Codierung
         /// </summary>
         /// <param name="data"></param>
         void EncodeLength0(int data) {
            EncodeLength(2 * Math.Abs(data) - (Math.Sign(data) + 1) / 2);
         }

         /// <summary>
         /// speichert den Wert in C1-Codierung
         /// </summary>
         /// <param name="data"></param>
         void EncodeLength1(int data) {
            EncodeLength(2 * Math.Abs(data - 1) + (Math.Sign(data - 1) - 1) / 2);
         }

         /// <summary>
         /// erzeugt ein Plateau der vorgegebenen Länge
         /// </summary>
         /// <param name="length"></param>
         /// <param name="startcolumn"></param>
         /// <param name="startline"></param>
         /// <param name="linelength"></param>
         void EncodePlateau(int length, int startcolumn, int startline, int linelength) {
            BitUnits = new List<char>();

            bool bLineEnd = false;
            int lastunitidx = -1;

            if (lastplateauendline != startline) {
               if (PlateauTabPointer4Unit > 0)
                  PlateauTabPointer4Unit--; // wenn min. 1 Zeilenwechsel ohne Plateau erfolgte
               lastplateauendline = startline;
            }

            int bitunit = 0;
            do {
               bitunit = PlateauBitUnitTable[PlateauTabPointer4Unit];

               if (bitunit == 0x08)
                  Lines4Unit8.Add(startline);

               if (bitunit <= length ||
                   startcolumn + length > linelength) { // mit Zeilenwechsel
                  switch (bitunit) {
                     case 0x1: BitUnits.Add('1'); break;
                     case 0x2: BitUnits.Add('2'); break;
                     case 0x4: BitUnits.Add('4'); break;
                     case 0x8: BitUnits.Add('8'); break;
                     case 0x10: BitUnits.Add('6'); break;
                     case 0x20: BitUnits.Add('3'); break;
                     case 0x40: BitUnits.Add('L'); break;
                  }

                  Bits.Add(1);
                  lastunitidx = PlateauTabPointer4Unit++;
                  PlateauTabPointer4Bits++;

                  length -= bitunit;
                  startcolumn += bitunit;

                  bLineEnd = false;

                  if (startcolumn >= linelength) { // erreicht das Zeilenende oder geht darüber hinaus -> Korrektur
                     bLineEnd = true;
                     startline++;
                     lastplateauendline++;

                     if (startcolumn > linelength &&
                         PlateauTabPointer4Unit > 1) {
                        PlateauTabPointer4Unit--;
                        PlateauTabPointer4Bits--;
                     }

                     length += startcolumn - linelength;
                     startcolumn = 0;
                  }
               } else
                  break;

            } while (true);

            if (Bits.Count > 0) {
               if (PlateauTabPointer4Unit > 0)
                  PlateauTabPointer4Unit--; // Endposition ist neue Startpos.
            }
            if (PlateauTabPointer4Bits > 0)
               PlateauTabPointer4Bits--; // Sollte besser nur so erfolgen, dass hinreichend viele Bits entsprechend PlateauTabPointer4Unit übrig bleiben?!?!!

            int bitsdelta = 0;
            if (bLineEnd) { // wenn am Zeilenende die LETZTE 1er, 2er, 4er oder 8er Unit verwendet wurde, wird akt. 1 Bit weniger im Binärteil verwendet
               if (lastunitidx == 3 ||
                   lastunitidx == 7 ||
                   lastunitidx == 11 ||
                   lastunitidx == 15)
                  bitsdelta = -1;
            }
            if (PlateauTabPointer4Unit >= 16) { // 16er Unit beginnt
               if (Lines4Unit8.Count > 4 || // 8er Unit wurde mehr als 4x ...
                   (Lines4Unit8.Count == 4 && Lines4Unit8[0] != Lines4Unit8[3])) { // ... oder genau 4x aber auf unterschiedlichen Zeilen verwendet
                  PlateauTabPointer4Bits++;
                  Lines4Unit8.Clear();
               }
            }


            // Basis abschließen (Trennbit)
            Bits.Add(0);

            // Rest binär codieren
            if (PlateauBitTable[PlateauTabPointer4Bits] > 0)
               EncodeBinary((uint)length, PlateauBitTable[PlateauTabPointer4Bits + bitsdelta]);
         }


         /// <summary>
         /// liefert den Exponent n von 2^n oder -1, wenn hunit nicht 2^n ist
         /// </summary>
         /// <param name="hunit"></param>
         /// <returns></returns>
         static int HunitExponent(int hunit) {
            for (int i = 0; i < 32; i++) {
               int t = 1 << i;
               if ((hunit & t) != 0) {
                  if ((hunit & ~t) != 0)
                     return -1;
                  return i;
               }
            }
            return -1;
         }

         /// <summary>
         /// liefert die max. mögliche Anzahl 0-Bits für die Hybridcodierung
         /// </summary>
         /// <param name="maxheigth"></param>
         /// <returns></returns>
         static int GetMaxLengthBits(int maxheigth) {
            if (maxheigth < 2)
               return 15;
            if (maxheigth < 4)
               return 16;
            if (maxheigth < 8)
               return 17;
            if (maxheigth < 16)
               return 18;
            if (maxheigth < 32)
               return 19;
            if (maxheigth < 64)
               return 20;
            if (maxheigth < 128)
               return 21;
            if (maxheigth < 256)
               return 22;
            if (maxheigth < 512)
               return 23;
            if (maxheigth < 1024)
               return 28;
            if (maxheigth < 2048)
               return 31;
            if (maxheigth < 4096)
               return 34;
            if (maxheigth < 8192)
               return 37;
            if (maxheigth < 16384)
               return 40;
            return 43;
         }

         /// <summary>
         /// liefert den erlaubten Wertebereich für eine Codierung und testet bei Bedarf einen Wert
         /// </summary>
         /// <param name="hunit">Heightunit oder 0 für Längencodierung</param>
         /// <param name="l0">wenn Heightunit gleich 0, dann L0 (true) oder L1 (false)</param>
         /// <param name="maxheigth">max. Höhe der Kachel</param>
         /// <param name="min">kleinster Wert</param>
         /// <param name="max">größter Wert</param>
         /// <param name="except">Liste von Ausnahmen im erlaubten Wertebereich</param>
         /// <param name="plateaufollower">wenn true, dann für einen Plateaunachfolger</param>
         /// <param name="testvalue">ein Wert der getestet wird</param>
         /// <returns>Testergebnis für einen Wert</returns>
         public static bool GetPermittedValues(int hunit, bool l0, int maxheigth, out int min, out int max, out List<int> except, bool plateaufollower = false, int testvalue = int.MinValue) {
            min = int.MinValue;
            max = int.MaxValue;
            except = new List<int>();

            int maxlenbit = GetMaxLengthBits(maxheigth);
            if (plateaufollower) // für Plateaunachfolger 1 Bit weniger
               maxlenbit--;
            if (hunit <= 0) {
               if (l0) {
                  min = -maxlenbit / 2;
                  max = (maxlenbit - 1) / 2;
               } else {
                  min = -(maxlenbit - 1) / 2;
                  max = (maxlenbit + 2) / 2;
               }
            } else {
               max = maxlenbit * hunit; // Längencodierung
               max += hunit - 1;
               max++;
               min = -(max - 1);
            }

            if (testvalue != int.MinValue)
               if (testvalue < min || testvalue > max || except.Contains(testvalue))
                  return false;
            return true;
         }

         /// <summary>
         /// erzeugt die statischen Plateaudaten
         /// </summary>
         static void BuildPlateauTables() {
            PlateauBitUnitTable = new List<int>();
            PlateauBitTable = new List<int>();

            for (int i = 0; i < 4; i++) PlateauBitUnitTable.Add(1);
            for (int i = 0; i < 4; i++) PlateauBitUnitTable.Add(2);
            for (int i = 0; i < 4; i++) PlateauBitUnitTable.Add(4);
            for (int i = 0; i < 4; i++) PlateauBitUnitTable.Add(8);
            for (int i = 0; i < 3; i++) PlateauBitUnitTable.Add(16);
            for (int i = 0; i < 3; i++) PlateauBitUnitTable.Add(32);
            for (int i = 0; i < 64; i++) PlateauBitUnitTable.Add(64);

            for (int i = 0; i < 3; i++) PlateauBitTable.Add(0);
            for (int i = 0; i < 4; i++) PlateauBitTable.Add(1);
            for (int i = 0; i < 4; i++) PlateauBitTable.Add(2);
            for (int i = 0; i < 4; i++) PlateauBitTable.Add(3);
            for (int i = 0; i < 3; i++) PlateauBitTable.Add(4);
            for (int i = 0; i < 3; i++) PlateauBitTable.Add(5);
            for (int i = 0; i < 2; i++) PlateauBitTable.Add(6);
            for (int i = 0; i < 64; i++) PlateauBitTable.Add(7);
         }

         public override string ToString() {
            StringBuilder sb = new StringBuilder(ElementTyp.ToString());
            if (Column >= 0)
               sb.Append(", Column=" + Column.ToString());
            if (Line >= 0)
               sb.Append(", Line=" + Line.ToString());
            sb.Append(", Data=" + Data.ToString());
            if (this.ElementTyp == Typ.Plateau)
               sb.Append(", PlateauTabColPos=" + PlateauTabPointer4Unit.ToString());
            sb.Append(", Encoding=" + Encoding.ToString());
            if (Encoding == EncodeMode.Hybrid)
               sb.Append(" (" + HUnit.ToString() + ")");
            sb.Append(", Bits=");
            for (int i = 0; i < Bits.Count; i++)
               sb.Append(Bits[i] > 0 ? "1" : ".");
            if (this.ElementTyp == Typ.Plateau)
               sb.Append(" " + GetBitUnitsText());
            else if (this.ElementTyp == Typ.PlateauFollower)
               sb.Append(" ddiff=" + PlateauFollowerDdiff);

            return sb.ToString();
         }

      }


      abstract class CodingType {

         /// <summary>
         /// akt. Codierungsart
         /// </summary>
         public HeightElement.EncodeMode EncodeMode { get; protected set; }

         /// <summary>
         /// liefert den Wert (immer einer 2er-Potenz)
         /// </summary>
         public int HunitValue { get; protected set; }

         /// <summary>
         /// liefert den Exponent der 2er-Potenz zu <see cref="HunitValue"/>
         /// </summary>
         public int HunitExponent { get; protected set; }

         /// <summary>
         /// akt. Summe für die Hybridcodierung
         /// </summary>
         public int SumH { get; protected set; }

         /// <summary>
         /// akt. Summe der Bewertungen für die Art der Längencodierung
         /// </summary>
         public int SumL { get; protected set; }

         /// <summary>
         /// akt. Anzahl Elemente
         /// </summary>
         public int ElemCount { get; protected set; }

         protected int maxheigthdiff = 0;


         public CodingType() {
            EncodeMode = HeightElement.EncodeMode.Hybrid;
            SumH = SumL = 0;
            ElemCount = 0;
            HunitValue = 0;
            HunitExponent = 0;
         }

         /// <summary>
         /// bildet <see cref="CodingType"/> für die Hybridcodierung auf der Basis des hunit-Wertes
         /// </summary>
         /// <param name="hunitvalue">für diesen Wert</param>
         /// <param name="ismaxheight">wenn true wird die max. Höhendiff. geliefert</param>
         public CodingType(int hunitvalue, bool ismaxheight) : this() {
            SumH = 0;
            ElemCount = 0;
            if (hunitvalue < 0)
               throw new Exception("Der Wert von hunitvalue kann nicht kleiner 0 sein.");
            if (ismaxheight) {
               maxheigthdiff = hunitvalue;
               HunitValue = GetHeightUnit4MaxHeight(hunitvalue);
            } else
               SetHunitValue(hunitvalue);
         }

         /// <summary>
         /// bildet <see cref="CodingType"/> für die Hybridcodierung auf der Basis des Exponenten für die 2er-Potenz
         /// </summary>
         /// <param name="exponent"></param>
         public CodingType(int exponent) : this() {
            SumH = 0;
            ElemCount = 0;
            if (exponent == 0)
               SetHunitValue(0);
            else {
               if (exponent < 0)
                  throw new Exception("Der Exponent von hunitvalue kann nicht kleiner 0 sein.");
               int tmp = 1;
               while (--exponent > 0)
                  tmp *= 2;
               SetHunitValue(tmp);
            }
         }

         /// <summary>
         /// erzeugt eine Kopie
         /// </summary>
         /// <param name="ct"></param>
         public CodingType(CodingType ct) {
            maxheigthdiff = ct.maxheigthdiff;
            HunitValue = ct.HunitValue;
            HunitExponent = ct.HunitValue;
            SumH = ct.SumH;
            SumL = ct.SumL;
            ElemCount = ct.ElemCount;
            EncodeMode = ct.EncodeMode;
         }

         abstract public void AddValue(int data);

         /// <summary>
         /// setzt die <see cref="HeightUnit"/> neu
         /// </summary>
         /// <param name="abssum">bisherige Summe</param>
         /// <param name="elemcount">Anzahl der Elemente</param>
         /// <param name="bStd">Standardverfahren (sonst für Plateau)</param>
         /// <param name="maxheigthdiff">falls abweichend vom globalen Wert</param>
         protected void SetHunit4SumAndElemcount(int abssum, int elemcount, bool bStd = true, int maxheigthdiff = int.MinValue) {
            int counter = abssum + GetHeightUnitTableDelta4MaxHeight(maxheigthdiff > 0 ? maxheigthdiff : this.maxheigthdiff);
            int denominator = elemcount;
            if (counter >= denominator) {
               if (bStd)
                  SetHunitValue((counter + 1) / (denominator + 1));
               else
                  SetHunitValue((counter + 1 + elemcount) / (2 * (denominator + 1)));     // für Plateau0
            } else
               SetHunitValue(0);
         }

         /// <summary>
         /// liefert den <see cref="HeightUnit"/>-Wert für die max. Höhendiff.
         /// </summary>
         /// <param name="maxheigth"></param>
         /// <returns></returns>
         static public int GetHeightUnit4MaxHeight(int maxheigth) {
            if (maxheigth < 0x9f)
               return 1;
            else if (maxheigth < 0x11f)
               return 2;
            else if (maxheigth < 0x21f)
               return 4;
            else if (maxheigth < 0x41f)
               return 8;
            else if (maxheigth < 0x81f)
               return 16;
            else if (maxheigth < 0x101f)
               return 32;
            else if (maxheigth < 0x201f)
               return 64;
            else if (maxheigth < 0x401f)
               return 128;
            return 256;
         }

         /// <summary>
         /// liefert die "waagerechte" Verschiebung für die Abssum-Tabelle für die max. Höhendiff. (0, ...)
         /// </summary>
         /// <param name="maxheigth">max. Höhendiff.</param>
         /// <returns></returns>
         static public int GetHeightUnitTableDelta4MaxHeight(int maxheigth) {
            return (Math.Max(0x5f, maxheigth) - 0x5f) / 0x40;
         }

         /// <summary>
         /// Bewertung des neuen Wertes für Längencodierung
         /// </summary>
         /// <param name="oldsum">bisherige Summe</param>
         /// <param name="elemcount">bisher registrierte Elementanzahl</param>
         /// <param name="newdata">neuer Wert</param>
         /// <returns></returns>
         protected int EvaluateData(int oldsum, int elemcount, int newdata) {
            /*
               D < -2 – (ls + 3*k)/2   1 – ls – k	
               D < 0 – (ls + k)/2      2*(d + k) + 3	
               D < 2 – (ls – k)/2      2*d – 1	
               D < 4 – (ls – 3*k)/2    2*(d – k) - 5	
                                       1 – ls + k	
             */

            int v = 0;
            if (newdata < (-4 - oldsum - 3 * elemcount) / 2) {
               v = -1 - oldsum - elemcount;
            } else if (newdata < -(oldsum + elemcount) / 2) {
               v = 2 * (newdata + elemcount) + 3;
            } else if (newdata < (4 - oldsum + elemcount) / 2) {
               v = 2 * newdata - 1;
            } else if (newdata < (8 - oldsum + 3 * elemcount) / 2) {
               v = 2 * (newdata - elemcount) - 5;
            } else {
               v = 1 - oldsum + elemcount;
            }
            return v;
         }

         /// <summary>
         /// setzt <see cref="CodingType"/> für die Hybridcodierung auf die größte 2er-Potenz, die nicht größer als <see cref="value"/> ist
         /// </summary>
         /// <param name="hunitvalue"></param>
         protected void SetHunitValue(int hunitvalue) {
            hunitvalue &= 0x1FF;
            // höchstes 1-Bit suchen
            HunitValue = 0x100;
            HunitExponent = 8;
            while (HunitValue > 0 &&
                   (HunitValue & hunitvalue) == 0) {
               HunitValue >>= 1;
               HunitExponent--;
            }
         }

         public override string ToString() {
            StringBuilder sb = new StringBuilder(EncodeMode.ToString());
            sb.Append(": ElemCount=");
            sb.Append(ElemCount.ToString());
            if (EncodeMode == HeightElement.EncodeMode.Hybrid) {
               sb.Append(", HunitValue=");
               sb.Append(HunitValue.ToString());
               sb.Append(", HunitExponent=");
               sb.Append(HunitExponent.ToString());
               sb.Append(", SumH=");
               sb.Append(SumH.ToString());
            } else {
               sb.Append(", SumL=");
               sb.Append(SumL.ToString());
            }
            return sb.ToString();
         }

      }

      class CodingTypeStd : CodingType {

         /// <summary>
         /// bildet <see cref="CodingType"/> für die Hybridcodierung auf der Basis des hunit-Wertes
         /// </summary>
         /// <param name="hunitvalue">für diesen Wert</param>
         /// <param name="ismaxheight">wenn true wird die max. Höhendiff. geliefert</param>
         public CodingTypeStd(int hunitvalue) : base(hunitvalue, true) { }

         /// <summary>
         /// erzeugt eine Kopie
         /// </summary>
         /// <param name="ct"></param>
         public CodingTypeStd(CodingTypeStd ct) : base(ct) { }


         override public void AddValue(int data) {
            // ---- SumH aktualisieren ----
            SumH += Math.Abs(data);

            // ---- SumL aktualisieren ----

            if (data < 0) {
               Debug.WriteLine("");
            }

            int eval = EvaluateData(SumL, ElemCount, data);
            if (ElemCount == 63) { // Sonderfall
               if (data < 0) {
                  if (2 * data + SumL == -65)
                     eval = 2 * data - 1;
               } else { // data > 0
                  if (2 * data + SumL == 69)
                     eval = 2 * data - 1;
               }
            }
            SumL += eval;

            // ---- ElemCount aktualisieren ----
            ElemCount++;

            // ---- Korrektur der Werte bei großem ElemCount ----
            if (ElemCount == 64) {
               ElemCount = 32;

               SumH = (SumH / 2) - 1;

               SumL /= 2;
               if (SumL % 2 != 0)
                  SumL++;
               else {
                  if (data < -1 || 1 < data)
                     if (SumL % 2 == 0)
                        SumL += 2;
               }
            }

            // ---- Hunit ermitteln ----
            SetHunit4SumAndElemcount(SumH, ElemCount);

            // ---- EncodeMode neu setzen ----
            if (HunitValue > 0)
               EncodeMode = HeightElement.EncodeMode.Hybrid;
            else
               EncodeMode = SumL > 0 ? HeightElement.EncodeMode.Length1 : HeightElement.EncodeMode.Length0;

            Debug.WriteLine(this);

         }

      }

      class CodingTypePlateau : CodingType {

         /// <summary>
         /// bildet <see cref="CodingType"/> für die Hybridcodierung auf der Basis des hunit-Wertes
         /// </summary>
         /// <param name="hunitvalue">für diesen Wert</param>
         /// <param name="ismaxheight">wenn true wird die max. Höhendiff. geliefert</param>
         public CodingTypePlateau(int hunitvalue) : base(hunitvalue, true) { }

         /// <summary>
         /// erzeugt eine Kopie
         /// </summary>
         /// <param name="ct"></param>
         public CodingTypePlateau(CodingTypePlateau ct) : base(ct) { }


         override public void AddValue(int data) {
            // ---- SumH aktualisieren ----
            SumH += Math.Abs(data);

            // ---- SumL aktualisieren ----
            int eval = EvaluateData(SumL, ElemCount, data);
            if (ElemCount == 63) { // Sonderfall
               if (data < 0) {
                  if (2 * data + SumL == -65)
                     eval = 2 * data - 1;
               } else { // data > 0
                  if (2 * data + SumL == 69)
                     eval = 2 * data - 1;
               }
            }
            SumL += eval;

            // ---- ElemCount aktualisieren ----
            ElemCount++;

            // ---- Korrektur der Werte bei großem ElemCount ----
            if (ElemCount == 64) {
               ElemCount = 32;

               SumH = (SumH / 2) - 1;

               SumL /= 2;
               if (SumL % 2 != 0)
                  SumL++;
            }

            // ---- Hunit ermitteln ----
            SetHunit4SumAndElemcount(SumH, ElemCount);

            // ---- EncodeMode neu setzen ----
            if (HunitValue > 0)
               EncodeMode = HeightElement.EncodeMode.Hybrid;
            else
               EncodeMode = SumL > 0 ? HeightElement.EncodeMode.Length1 : HeightElement.EncodeMode.Length0;
         }

      }

      class CodingTypePlateau0 : CodingType {

         int bonus = 1;

         /// <summary>
         /// bildet <see cref="CodingType"/> für die Hybridcodierung auf der Basis des hunit-Wertes
         /// </summary>
         /// <param name="hunitvalue">für diesen Wert</param>
         /// <param name="ismaxheight">wenn true wird die max. Höhendiff. geliefert</param>
         public CodingTypePlateau0(int hunitvalue) : base(hunitvalue, true) {
            SumH = 2;
         }

         /// <summary>
         /// erzeugt eine Kopie
         /// </summary>
         /// <param name="ct"></param>
         public CodingTypePlateau0(CodingTypePlateau0 ct) : base(ct) { }


         override public void AddValue(int data) {
            // ---- SumH aktualisieren ----
            if (data > 0)
               SumH += 2 * (data - 1);
            else
               SumH -= 2 * data;

            // ---- SumL aktualisieren ----
            int eval = EvaluateData(SumL, ElemCount, data);
            if (eval > 0)
               eval = 1;
            else if (eval < 0)
               eval = -1;

            if (ElemCount == 63) { // Sonderfall
               if (data < 0) {
                  if (2 * data + SumL == -65)
                     eval = 2 * data - 1;
               } else { // data > 0
                  if (2 * data + SumL == 69)
                     eval = 2 * data - 1;
               }
            }
            SumL += eval;

            if (ElemCount == 0) {
               if (data == 2)
                  bonus = 3;
            }

            // ---- ElemCount aktualisieren ----
            ElemCount++;

            // ---- Korrektur der Werte bei großem ElemCount ----
            if (ElemCount == 64) {
               ElemCount = 32;

               SumH = (SumH / 2) - 1;

               SumL /= 2;
               if (SumL % 2 != 0)
                  SumL++;
            }

            // ---- Hunit ermitteln ----
            SetHunit4SumAndElemcount(SumH, ElemCount, false);

            // ---- EncodeMode neu setzen ----
            if (HunitValue > 0)
               EncodeMode = HeightElement.EncodeMode.Hybrid;
            else
               EncodeMode = SumL + bonus > 0 ? HeightElement.EncodeMode.Length1 : HeightElement.EncodeMode.Length0;


            //Debug.WriteLine(this);

         }

      }

      #endregion


      /// <summary>
      /// nächste Position für Höhen und Daten
      /// </summary>
      Position nextPosition;

      /// <summary>
      /// akt. Codierungs-Art (Codierung, die vom letzten <see cref="HeightElement"/> geliefert wird)
      /// </summary>
      public HeightElement.EncodeMode ActualMode {
         get {
            return Elements.Count > 0 ?
                        Elements[Elements.Count - 1].Encoding :
                        HeightElement.EncodeMode.notdefined;
         }
      }

      /// <summary>
      /// max. zulässige Höhe der Kachel
      /// </summary>
      public int MaxHeigth { get; protected set; }

      /// <summary>
      /// Kachelgröße
      /// </summary>
      public int TileSize { get; protected set; }

      /// <summary>
      /// zur Bestimmung der Heightunit für die Gruppe der Standardwerte
      /// </summary>
      CodingTypeStd hu_std;

      /// <summary>
      /// zur Bestimmung der Heightunit für die Gruppe der Plateau-Nachfolger mit ddiff=0
      /// </summary>
      CodingTypePlateau0 hu_ddiff4plateaufollower_zero;

      /// <summary>
      /// zur Bestimmung der Heightunit für die Gruppe der Plateau-Nachfolger mit ddiff!=0
      /// </summary>
      CodingTypePlateau hu_ddiff4plateaufollower_notzero;


      /// <summary>
      /// Liste der registrierten Höhenelemente
      /// </summary>
      public List<HeightElement> Elements { get; private set; }

      CodingTypeStd _initialHeigthUnit;

      /// <summary>
      /// hunit bei der Init. des Encoders (abh. von der Maximalhöhe; konstant; max. 256)
      /// </summary>
      public int InitialHeigthUnit {
         get {
            return _initialHeigthUnit.HunitValue;
         }
         private set {
            _initialHeigthUnit = new CodingTypeStd(value);
         }
      }

      /// <summary>
      /// akt. hunit
      /// </summary>
      public int HeigthUnit { get; private set; }

      /// <summary>
      /// liefert die akt. Höhe
      /// </summary>
      public int ActualHeigth {
         get {
            return nextPosition.Idx > 0 ? HeightValues[nextPosition.Idx - 1] : 0;
         }
      }

      /// <summary>
      /// Standardhöhe der akt. Zeile
      /// </summary>
      public int StdHeigth { get; protected set; }

      /// <summary>
      /// Zeile der nächsten Höhe
      /// </summary>
      public int NextHeigthLine {
         get {
            return nextPosition.Y;
         }
      }

      /// <summary>
      /// Spalte der nächsten Höhe
      /// </summary>
      public int NextHeigthColumn {
         get {
            return nextPosition.X;
         }
      }

      /// <summary>
      /// alle Höhenwerte
      /// </summary>
      public List<int> HeightValues { get; protected set; }


      /// <summary>
      /// erzeugt einen Encoder für eine Kachel
      /// <para>Mit dem wiederholten Aufruf von <see cref="ComputeNext"/> werden die gelieferten Daten schrittweise codiert. Wenn diese Funktion 0 liefert, ist
      /// die Codierung abgeschlossen. Die fertige Bytefolge knn mit <see cref="GetCodedBytes"/> abgerufen werden.
      /// </para>
      /// </summary>
      /// <param name="maxheigth">max. Höhe</param>
      /// <param name="tilesize">Breite/Höhe der Kachel</param>
      /// <param name="height">Liste der Höhendaten (Anzahl normalerweise <see cref="tilesize"/> * <see cref="tilesize"/>)</param>
      public TileEncoder(int maxheigth, int tilesize, IList<int> height) {
         MaxHeigth = maxheigth;
         TileSize = tilesize;
         StdHeigth = 0;
         HeightValues = new List<int>(height);
         nextPosition = new Position(tilesize, tilesize);

         hu_std = new CodingTypeStd(MaxHeigth);
         hu_ddiff4plateaufollower_zero = new CodingTypePlateau0(MaxHeigth);
         hu_ddiff4plateaufollower_notzero = new CodingTypePlateau(MaxHeigth);

         Elements = new List<HeightElement>();

         InitialHeigthUnit = MaxHeigth; // die korrekte hunit wird intern bestimmt

      }

      /// <summary>
      /// liefert die codierte Bitfolge als Bytes
      /// </summary>
      /// <param name="fillbits">Auffüllbits für das letzte Byte (1 wenn true)</param>
      /// <returns></returns>
      public byte[] GetCodedBytes(bool fillbits = true) {
         List<byte> Bits = new List<byte>();
         foreach (var item in Elements)
            Bits.AddRange(item.Bits);

         byte[] data = new byte[Bits.Count % 8 == 0 ? Bits.Count / 8 : Bits.Count / 8 + 1];

         for (int i = 0; i < Bits.Count; i += 8) {
            int bidx = i / 8;
            for (int j = 0; j < 8 && i + j < Bits.Count; j++) {
               switch (j) {
                  case 0: data[bidx] |= (byte)(Bits[i + j] << 7); break;
                  case 1: data[bidx] |= (byte)(Bits[i + j] << 6); break;
                  case 2: data[bidx] |= (byte)(Bits[i + j] << 5); break;
                  case 3: data[bidx] |= (byte)(Bits[i + j] << 4); break;
                  case 4: data[bidx] |= (byte)(Bits[i + j] << 3); break;
                  case 5: data[bidx] |= (byte)(Bits[i + j] << 2); break;
                  case 6: data[bidx] |= (byte)(Bits[i + j] << 1); break;
                  case 7: data[bidx] |= (byte)(Bits[i + j]); break;
               }
            }
         }
         if (fillbits &&
             data.Length > 0) {
            switch (Bits.Count % 8) {
               case 0: break;
               case 1: data[data.Length - 1] |= 0x7f; break;
               case 2: data[data.Length - 1] |= 0x3f; break;
               case 3: data[data.Length - 1] |= 0x1f; break;
               case 4: data[data.Length - 1] |= 0x0f; break;
               case 5: data[data.Length - 1] |= 0x07; break;
               case 6: data[data.Length - 1] |= 0x03; break;
               case 7: data[data.Length - 1] |= 0x01; break;
            }
         }

         return data;
      }

      /// <summary>
      /// liefert die codierte Bitfolge als Zeichenkette mit '1' und '.'
      /// </summary>
      /// <param name="from"></param>
      /// <param name="length"></param>
      /// <returns></returns>
      public string GetBinText(int from = 0, int length = 0) {
         StringBuilder sb = new StringBuilder();

         List<byte> Bits = new List<byte>();
         foreach (var item in Elements)
            Bits.AddRange(item.Bits);

         if (from < 0)
            from = 0;
         if (length <= 0 || Bits.Count - from < length)
            length = Bits.Count - from;
         for (int i = from; i < from + length; i++)
            sb.Append(Bits[i] != 0 ? "1" : "0");
         sb.Replace('0', '.');
         return sb.ToString();
      }

      /// <summary>
      /// führt die Codierung der nächsten Höhe/n aus
      /// <para>I.A. wird hierbei der nächste Höhenwert codiert. Es können aber auch mehrere Höhenwerte gemeinsam codiert werden, wenn dass wegen des 
      /// Codierverfahrens nötig ist.</para>
      /// </summary>
      /// <param name="bTileIsFull">true, wenn die Kachel gefüllt ist</param>
      /// <returns>Anzahl der im aktuellen Schritt codierten Elemente (0 bedeutet Ende der Codierung)</returns>
      public int ComputeNext(out bool bTileIsFull) {
         int elements = Elements.Count;
         bTileIsFull = true;
         if (nextPosition.Idx < HeightValues.Count)
            bTileIsFull = CalculateData(nextPosition);      // die akt. Höhe oder zusätzlich mehrere folgende Höhen codieren
         return Elements.Count - elements; ;
      }


      /// <summary>
      /// 
      /// </summary>
      /// <param name="pos"></param>
      /// <returns>true, wenn die Endpos. rechts-unten erreicht wurde</returns>
      bool CalculateData(Position pos) {
         bool bEnd = false;

         if (ValidHeight(pos) >= 0) { // nur nichtnegative Höhen

            try {

               int ddiff = ValidHeightDDiff(pos);

               if (ddiff == 0) { // die Diagonale hat konstante Höhe (gilt auch für die 1. Spalte) -> immer Plateau (ev. auch mit Länge 0)
                  int length;
                  bEnd = GetPlateauLength(pos, out length);
                  //if (!bEnd)
                  bEnd = WritePlateau(length, pos, hu_ddiff4plateaufollower_zero, hu_ddiff4plateaufollower_notzero);

               } else { // "Normalfall"

                  /* Vermutung für Normalfall: d(i,n) = -sgn(ddiff(i, n)) * (hdiff(i, n) - hdiff(i, n-1)))
                   * 
                   *    * * A B *
                   *    * * C X
                   */
                  int hdiff_up = ValidHeightHDiff(pos.X, pos.Y - 1); // horiz. Diff. der Höhe in der Zeile "über" der akt. Pos.: B - A
                  int data = ValidHeight(pos, -1) < -hdiff_up ?
                                                   -Math.Sign(ddiff) * ValidHeight(pos) :                  // C < A-B     -sgn(B-C)*X
                                                   -Math.Sign(ddiff) * (ValidHeightHDiff(pos) - hdiff_up);   // C >= A-B    -sgn(B-C)*(X-C-(B-A)) = -sgn(B-C)*X + sgn(B-C)(B+C-A)


                  // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                  /* vorläufig als Spezialfall: h(col+1,n-1) = h(col+2,n-2)

                   *    * * A
                   *    * A *
                   *    ?
                  */
                  //if (pos.Y > 0 && ValidHeight(pos.X + 1, pos.Y - 1) == ValidHeight(pos.X + 2, pos.Y - 2))
                  //   if (HeigthDDiff(pos) > 0) {
                  //      data = -Height(pos) + 1;
                  //   } else {
                  //      data = Height(pos);
                  //   }
                  // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

                  /*    * A
                   *    A *
                   *    ?
                   */
                  //if (pos.Y == 1) {
                  //   if (ValidHeight(pos.X, pos.Y - 1) == ValidHeight(pos.X + 1, pos.Y - 2)) {
                  //      int testvdiff = HeigthVDiff(pos.X - 1, pos.Y);
                  //      if (testvdiff > 0)
                  //         data = Height(pos) - testvdiff;
                  //      else
                  //         data = HeigthVDiff(pos);
                  //   }
                  //}


                  /*    * 0
                   *    0 *
                   *    ?
                   */
                  if (ValidHeight(pos.X, pos.Y - 1) == 0 &&
                      ValidHeight(pos.X + 1, pos.Y - 2) == 0) {
                     if (ValidHeightVDiff(pos.X - 1, pos.Y) <= 0)
                        data = ValidHeightVDiff(pos);
                  }


                  AddHeightValue(data, pos, hu_std);

                  bEnd = !pos.MoveForward();

                  //Debug.WriteLine(data.ToString() + ", " + lengthtype_std.ToString());
               }

            } catch (Exception ex) {
               throw new Exception(string.Format("interner Fehler bei Position {0}, Höhe {1}: {2}", pos, ValidHeight(pos), ex.Message));
            }
         }
         return bEnd;
      }

      /// <summary>
      /// erzeugt ein <see cref="HeightElement"/> für die Plateaulänge und eins für den Plateaunachfolger
      /// </summary>
      /// <param name="length">Plateaulänge</param>
      /// <param name="pos">akt. Position</param>
      /// <param name="hunit_followerddiffzero">hunit-Berechnung wenn die ddiff für den Nachfolger 0 ist</param>
      /// <param name="hunit_followerddiffnotzero">hunit-Berechnung wenn die ddiff für den Nachfolger ungleich 0 ist</param>
      /// <returns>true, wenn die Endpos. rechts-unten erreicht wurde</returns>
      bool WritePlateau(int length, Position pos, CodingTypePlateau0 hunit_followerddiffzero, CodingTypePlateau hunit_followerddiffnotzero) {
         bool bEnd;
         // Plateaulänge codieren
         Elements.Add(HeightElement.CreateHeightElement_Plateau(length, pos.X, pos.Y, TileSize, Elements));

         // Nachfolgewert bestimmen
         bEnd = !pos.MoveForward(length);      // <-- PROBLEM am Ende einer Kachel ????

         if (!bEnd)
            if (pos.X != 0 ||       // Das ist die Position für den Follower!
                length == 0) {      // dann muss es immer einen Nachfolger geben
               int follower = ValidHeight(pos);
               int follower_ddiff = pos.X == 0 ?
                                          0 : // in der 1. Spalte ist die Berechnung anders
                                          ValidHeightDDiff(pos);
               int follower_vdiff = ValidHeightVDiff(pos);

               CodingType hu;

               // Nachfolger codieren
               if (follower_ddiff != 0) {

                  if (follower_ddiff > 0)
                     follower_vdiff *= -1;

                  hu = hunit_followerddiffnotzero;

               } else {

                  if (follower_vdiff < 0)
                     follower_vdiff++;

                  hu = hunit_followerddiffzero;

               }
               AddHeightValue(follower_vdiff, pos, hu, follower_ddiff);

               //Debug.WriteLine(follower_vdiff.ToString() + " -> " + hu.ToString());

               follower = follower_vdiff;

               // hinter den Nachfolger positionieren
               bEnd = !pos.MoveForward();
            }

         return bEnd;
      }

      /// <summary>
      /// ermittelt die Länge eines Plateaus ab der Startposition (ev. auch 0)
      /// </summary>
      /// <param name="pos">Startpos.</param>
      /// <param name="length">Länge</param>
      /// <returns>true, wenn die Endpos. rechts-unten erreicht wurde</returns>
      bool GetPlateauLength(Position startpos, out int length) {
         Position tst = new Position(startpos);
         length = 0;
         int value = -1;
         bool bEnd = false;

         while (tst.Idx < HeightValues.Count) {
            if (value < 0)
               value = ValidHeight(tst.X - 1, tst.Y);

            if (HeightValues[tst.Idx] != value)
               break;

            length++;
            if (!tst.MoveForward()) { // Ende erreicht
               bEnd = true;
               break;
            }

            if (tst.X == 0)
               value = -1;
         }

         return bEnd;
      }


      void AddHeightValue(int data, Position pos, CodingType hu, int plateauddiff = int.MinValue) {
         int min, max;
         List<int> except;
         bool plateaufollower = plateauddiff != int.MinValue;
         bool l0coding = hu.EncodeMode == HeightElement.EncodeMode.Length0;

         if (!plateaufollower) {

            if (hu.HunitValue >= 1) {       // dann Hybridcodierung

               if (HeightElement.GetPermittedValues(hu.HunitValue, false, MaxHeigth, out min, out max, out except, plateaufollower, data)) {
                  Elements.Add(HeightElement.CreateHeightElement_ValueH(data, MaxHeigth, hu.HunitValue, pos.X, pos.Y));
               } else {                // dann ist Spezialcodierung nötig
                  Elements.Add(HeightElement.CreateHeightElement_BigValue(data, MaxHeigth, pos.X, pos.Y));
               }

            } else {                   // dann Längencodierung

               if (HeightElement.GetPermittedValues(0, l0coding, MaxHeigth, out min, out max, out except, plateaufollower, data)) {
                  Elements.Add(HeightElement.CreateHeightElement_ValueL(data, l0coding, pos.X, pos.Y));
               } else {                // dann ist Spezialcodierung nötig
                  if (l0coding)
                     Elements.Add(HeightElement.CreateHeightElement_BigValue(data, MaxHeigth, pos.X, pos.Y));
                  else
                     Elements.Add(HeightElement.CreateHeightElement_BigValueL1(data, MaxHeigth, pos.X, pos.Y));
               }

            }

         } else {

            if (hu.HunitValue >= 1) {       // dann Hybridcodierung

               if (HeightElement.GetPermittedValues(hu.HunitValue, false, MaxHeigth, out min, out max, out except, plateaufollower, data)) {
                  Elements.Add(HeightElement.CreateHeightElement_PlateauFollowerH(data, MaxHeigth, hu.HunitValue, plateauddiff, pos.X, pos.Y));
               } else {                            // dann ist Spezialcodierung nötig
                  Elements.Add(HeightElement.CreateHeightElement_PlateauFollowerBigValue(data, MaxHeigth, plateauddiff, pos.X, pos.Y));
               }

            } else {

               if (HeightElement.GetPermittedValues(0, l0coding, MaxHeigth, out min, out max, out except, plateaufollower, data)) {
                  Elements.Add(HeightElement.CreateHeightElement_PlateauFollowerL(data, l0coding, plateauddiff, pos.X, pos.Y));
               } else {                            // dann ist Spezialcodierung nötig
                  Elements.Add(HeightElement.CreateHeightElement_PlateauFollowerBigValue(data, MaxHeigth, plateauddiff, pos.X, pos.Y));
               }

            }

         }

         hu.AddValue(data);
      }


      #region spezielle Höhendifferenzen

      /// <summary>
      /// liefert die horizontale Höhendifferenz (zur Vorgängerhöhe)
      /// </summary>
      /// <param name="pos"></param>
      /// <returns></returns>
      int ValidHeightHDiff(Position pos) {
         return ValidHeightHDiff(pos.X, pos.Y);
      }

      /// <summary>
      /// liefert die horizontale Höhendifferenz (zur Vorgängerhöhe)
      /// </summary>
      /// <param name="col"></param>
      /// <param name="line"></param>
      /// <returns></returns>
      int ValidHeightHDiff(int col, int line) {
         return ValidHeight(col, line) - ValidHeight(col - 1, line);
      }

      /// <summary>
      /// liefert die vertikale Höhendifferenz (zur darüber liegenden Höhe)
      /// </summary>
      /// <param name="pos"></param>
      /// <returns></returns>
      int ValidHeightVDiff(Position pos) {
         return ValidHeightVDiff(pos.X, pos.Y);
      }

      /// <summary>
      /// liefert die vertikale Höhendifferenz (zur darüber liegenden Höhe)
      /// </summary>
      /// <param name="col"></param>
      /// <param name="line"></param>
      /// <returns></returns>
      int ValidHeightVDiff(int col, int line) {
         return ValidHeight(col, line) - ValidHeight(col, line - 1);
      }

      /// <summary>
      /// liefert die diagonale Höhendifferenz (der darüber liegenden Höhe zur Vorgängerhöhe)
      /// </summary>
      /// <param name="pos"></param>
      /// <returns></returns>
      int ValidHeightDDiff(Position pos) {
         return ValidHeightDDiff(pos.X, pos.Y);
      }

      /// <summary>
      /// liefert die diagonale Höhendifferenz (der darüber liegenden Höhe zur Vorgängerhöhe)
      /// </summary>
      /// <param name="col"></param>
      /// <param name="line"></param>
      /// <returns></returns>
      int ValidHeightDDiff(int col, int line) {
         return ValidHeight(col, line - 1) - ValidHeight(col - 1, line);
      }

      #endregion

      #region Zugriff auf Höhenwerte

      /// <summary>
      /// liefert die Höhe zum Index (alle Höhen sind zeilenweise hintereinander angeordnet)
      /// </summary>
      /// <param name="idx"></param>
      /// <returns>negativ, wenn ungültig</returns>
      int Height(int idx) {
         return 0 <= idx && idx < HeightValues.Count ?
                        HeightValues[idx] :
                        -1;
      }

      /// <summary>
      /// liefert die Höhe aus Spalte und Zeile (Spalte und Zeile müssen gültige Werte sein, da intern der Index verwendet wird)
      /// </summary>
      /// <param name="column"></param>
      /// <param name="line"></param>
      /// <returns>negativ, wenn ungültig</returns>
      public int Height(int column, int line) {
         return Height(TileSize * line + column);
      }

      /// <summary>
      /// liefert die Höhe bezüglich der Position (mit dem Delta muss sich eine gültige Position ergeben, da intern der Index verwendet wird)
      /// </summary>
      /// <param name="pos"></param>
      /// <param name="deltax">Spalten-Delta zur Position</param>
      /// <param name="deltay">Zeilen-Delta zur Position</param>
      /// <returns>negativ, wenn ungültig</returns>
      int ValidHeight(Position pos, int deltax = 0, int deltay = 0) {
         if (pos.X + deltax < 0 || pos.X + deltax >= pos.Width ||
             pos.Y + deltay < 0 || pos.Y + deltay >= pos.Height)
            return -1;
         return Height(new Position(pos, deltax, deltay).Idx);
      }

      /// <summary>
      /// liefert auch für ungültige Spalten und Zeilen eine verarbeitbare Höhe, d.h. außerhalb der <see cref="TileSize"/> immer 0 
      /// bzw. bei Spalte -1 die virtuelle Spalte (Spalte 0 der Vorgängerzeile)
      /// </summary>
      /// <param name="column"></param>
      /// <param name="line"></param>
      /// <returns></returns>
      public int ValidHeight(int column, int line) {
         if (0 <= column && column < TileSize &&
             0 <= line && line < TileSize) { // innerhalb des Standardbereiches
            int h = Height(column, line);
            if (h >= 0)
               return h;
         }
         if (column == -1 &&
             0 <= line && line < TileSize) { // virtuelle Spalte
            int h = Height(0, line - 1);
            return h >= 0 ? h : 0;
         }
         return 0;
      }

      #endregion

      #region zum Ermitteln der Bitfolgen

      static public List<byte> LengthCoding0(int data) {
         return new List<byte>(HeightElement.CreateHeightElement_ValueL(data, true, int.MinValue, int.MinValue).Bits);
      }

      static public List<byte> LengthCoding1(int data) {
         return new List<byte>(HeightElement.CreateHeightElement_ValueL(data, false, int.MinValue, int.MinValue).Bits);
      }

      static public List<byte> HybridCoding(int data, int maxheigth, int hunit) {
         return new List<byte>(HeightElement.CreateHeightElement_ValueH(data, maxheigth, hunit, int.MinValue, int.MinValue).Bits);
      }

      static public List<byte> BigValueCodingHybrid(int data, int maxheigth) {
         return new List<byte>(HeightElement.CreateHeightElement_BigValue(data, maxheigth, int.MinValue, int.MinValue).Bits);
      }

      static public List<byte> BigValueCodingLength0(int data, int maxheigth) {
         return new List<byte>(HeightElement.CreateHeightElement_BigValue(data, maxheigth, int.MinValue, int.MinValue).Bits);
      }

      static public List<byte> BigValueCodingLength1(int data, int maxheigth) {
         return new List<byte>(HeightElement.CreateHeightElement_BigValueL1(data, maxheigth, int.MinValue, int.MinValue).Bits);
      }

      #endregion

      public override string ToString() {
         return string.Format("MaxHeigth={0}, TileSize={1}, BaseHeigthUnit={2}, HeigthUnit={3}, ActualMode={4}, ActualHeigth={5}",
                              MaxHeigth,
                              TileSize,
                              InitialHeigthUnit,
                              HeigthUnit,
                              ActualMode,
                              ActualHeigth);
      }

   }
}