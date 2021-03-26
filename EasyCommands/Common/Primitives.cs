﻿using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public interface Primitive {
            PrimitiveType GetType();
            object GetValue();
            Primitive Plus(Primitive p);
            Primitive Minus(Primitive p);
            Primitive Multiply(Primitive p);
            Primitive Divide(Primitive p);
            Primitive Mod(Primitive p);
            Primitive Not();
            int Compare(Primitive p);
        }

        public static BooleanPrimitive CastBoolean(Primitive p) {
            switch (p.GetType()) {
                case PrimitiveType.BOOLEAN: return new BooleanPrimitive((bool)p.GetValue());
                case PrimitiveType.NUMERIC: return new BooleanPrimitive((float)p.GetValue() > 0);
                case PrimitiveType.STRING: return new BooleanPrimitive(bool.Parse((string)p.GetValue()));
                default: throw new Exception("Cannot convert Primitive Type: " + p.GetType() + " To Boolean");
            }
        }

        public static NumberPrimitive CastNumber(Primitive p) {
            switch (p.GetType()) {
                case PrimitiveType.BOOLEAN: return new NumberPrimitive((bool)p.GetValue() ? 1 : 0);
                case PrimitiveType.NUMERIC: return new NumberPrimitive((float)p.GetValue());
                case PrimitiveType.STRING: return new NumberPrimitive(float.Parse((string)p.GetValue()));
                case PrimitiveType.VECTOR: return new NumberPrimitive((float)((Vector3D)p.GetValue()).Length());
                default: throw new Exception("Cannot convert Primitive Type: " + p.GetType() + " To Boolean");
            }
        }

        public static StringPrimitive CastString(Primitive p) {
            switch (p.GetType()) {
                case PrimitiveType.VECTOR:
                    Vector3D vector = CastVector(p).GetVectorValue();
                    return new StringPrimitive(vector.X + ":" + vector.Y + ":" + vector.Z);
                default: return new StringPrimitive(p.GetValue().ToString());
            }
        }

        public static VectorPrimitive CastVector(Primitive p) {
            switch (p.GetType()) {
                case PrimitiveType.VECTOR: return new VectorPrimitive(((VectorPrimitive)p).GetVectorValue());
                case PrimitiveType.STRING:
                    Vector3D vector;
                    if (GetVector((String)p.GetValue(), out vector)) return new VectorPrimitive(vector);
                    goto default;
                default: throw new Exception("Cannot convert Primitive Type: " + p.GetType() + " To Vector");
            }
        }

        public class BooleanPrimitive : Primitive {
            private bool value;
            public BooleanPrimitive(bool value) { this.value = value; }
            public object GetValue() {
                return value;
            }

            public bool GetBooleanValue() {
                return value;
            }

            public Primitive Plus(Primitive p) {
                throw new Exception("Cannot Add Booleans");
            }

            public Primitive Minus(Primitive p) {
                throw new Exception("Cannot Subtract Booleans");
            }

            public Primitive Divide(Primitive p) {
                throw new Exception("Cannot Divide Booleans");
            }

            public Primitive Multiply(Primitive p) {
                throw new Exception("Cannot Multiply Booleans");
            }
            public Primitive Mod(Primitive p)
            {
                throw new Exception("Cannot Mod Booleans");
            }

            public Primitive Not() {
                return new BooleanPrimitive(!value);
            }

            PrimitiveType Primitive.GetType() {
                return PrimitiveType.BOOLEAN;
            }

            public int Compare(Primitive p) {
                return value.CompareTo(CastBoolean(p).GetBooleanValue());
            }

        }

        public class NumberPrimitive : Primitive {
            float number;

            public NumberPrimitive(float number) {
                this.number = number;
            }

            public object GetValue() {
                return number;
            }

            public float GetNumericValue() {
                return number;
            }

            public Primitive Plus(Primitive p) {
                return new NumberPrimitive(number + CastNumber(p).GetNumericValue());
            }

            public Primitive Divide(Primitive p) {
                return new NumberPrimitive(number / CastNumber(p).GetNumericValue());
            }

            public Primitive Minus(Primitive p) {
                return new NumberPrimitive(number - CastNumber(p).GetNumericValue());
            }

            public Primitive Multiply(Primitive p) {
                return new NumberPrimitive(number * CastNumber(p).GetNumericValue());
            }

            public Primitive Mod(Primitive p)
            {
                return new NumberPrimitive(number % CastNumber(p).GetNumericValue());
            }

            public Primitive Not() {
                return new NumberPrimitive(-number);
            }

            public int Compare(Primitive p) {
                return number.CompareTo(CastNumber(p).GetNumericValue());
            }

            PrimitiveType Primitive.GetType() {
                return PrimitiveType.NUMERIC;
            }

        }

        public class StringPrimitive : Primitive {
            String stringValue;

            public StringPrimitive(String value) {
                stringValue = value;
            }
            public int Compare(Primitive p) {
                return stringValue.CompareTo(CastString(p).GetStringValue());
            }

            public Primitive Divide(Primitive p) {
                throw new Exception("Cannot divide a string");
            }

            public object GetValue() {
                return stringValue;
            }

            public String GetStringValue() {
                return stringValue;
            }

            public Primitive Minus(Primitive p) {
                string newValue = "";
                if (p.GetType() == PrimitiveType.STRING) {
                    newValue = stringValue.Replace(CastString(p).stringValue, "");
                } else if (p.GetType()==PrimitiveType.NUMERIC) {
                    int removeLength = (int)p.GetValue();
                    if(removeLength < stringValue.Length) {
                        newValue = stringValue.Substring(0, removeLength);
                    }
                } else {
                    throw new Exception("Cannot subtract type: " + p.GetType() + " from string"); 
                }
                return new StringPrimitive(newValue);
            }

            public Primitive Multiply(Primitive p) {
                throw new Exception("Cannot multiply a string");
            }

            public Primitive Not() {
                throw new Exception("Cannot not a string");
                //Todo: String inverse?
            }

            public Primitive Plus(Primitive p) {
                return new StringPrimitive(stringValue + CastString(p).GetStringValue());
            }

            PrimitiveType Primitive.GetType() {
                return PrimitiveType.STRING;
            }

            public Primitive Mod(Primitive p)
            {
                throw new Exception("Cannot Mod a String");
            }
        }

        public class VectorPrimitive : Primitive {
            Vector3D vector;

            public VectorPrimitive(Vector3D vector) {
                this.vector = vector;
            }

            public int Compare(Primitive p) {
                switch(p.GetType()) {
                    case PrimitiveType.VECTOR:
                        return vector.Length().CompareTo(((Vector3D)p.GetValue()).Length());
                    case PrimitiveType.NUMERIC:
                        return ((float)vector.Length()).CompareTo(((float)p.GetValue()));
                    default: throw new Exception("Cannot compare vector to type: " + p.GetType());
                }
            }

            public Primitive Divide(Primitive p) {
                float length;
                switch (p.GetType()) {
                    case PrimitiveType.VECTOR:
                        length = (float)((Vector3D)p.GetValue()).Length();
                        break;
                    case PrimitiveType.NUMERIC:
                        length = (float)p.GetValue();
                        break;
                    default: throw new Exception("Cannot divide vector by type: " + p.GetType());
                }
                return new VectorPrimitive(Vector3D.Divide(vector, length));
            }

            public object GetValue() {
                return vector;
            }

            public Vector3D GetVectorValue() {
                return vector;
            }

            public Primitive Minus(Primitive p) {
                return new VectorPrimitive(Vector3.Subtract(vector, CastVector(p).GetVectorValue()));
            }

            //For a vector, modding a vector with another vector is asking to perform vector rejection.
            //https://en.wikipedia.org/wiki/Vector_projection
            public Primitive Mod(Primitive p) {
                Vector3D normalVector = CastVector(p).GetVectorValue();
                return new VectorPrimitive(Vector3D.Reject(vector, normalVector));
            }

            public Primitive Project(Primitive p) {
                Vector3D projectionVector = CastVector(p).GetVectorValue();
                return new VectorPrimitive(Vector3D.ProjectOnVector(ref vector, ref projectionVector));
            }

            public Primitive DotProduct(Primitive p) {
                Vector3D dotVector = CastVector(p).GetVectorValue();
                return new NumberPrimitive((float)Vector3D.Dot(vector, dotVector));
            }

            //For a vector, multiplying by another vector is asking to perform the cross product on the two
            public Primitive Multiply(Primitive p) {
                switch (p.GetType()) {
                    case PrimitiveType.VECTOR:
                        return new VectorPrimitive(vector.Cross((Vector3D)p.GetValue()));
                    case PrimitiveType.NUMERIC:
                        float length = (float)p.GetValue();
                        return new VectorPrimitive(Vector3D.Multiply(vector, length));
                    default: throw new Exception("Cannot multiply vector by type: " + p.GetType());
                }
            }

            public Primitive Not() {
                return new VectorPrimitive(Vector3D.Multiply(vector, -1));
            }

            public Primitive Plus(Primitive p) {
                return new VectorPrimitive(Vector3D.Add(vector, CastVector(p).GetVectorValue()));
            }

            PrimitiveType Primitive.GetType() {
                return PrimitiveType.VECTOR;
            }
        }

        //Add Color Primitive
    }
}
