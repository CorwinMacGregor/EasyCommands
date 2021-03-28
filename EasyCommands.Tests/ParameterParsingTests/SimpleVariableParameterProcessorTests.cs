﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using VRageMath;
using static IngameScript.Program;

namespace EasyCommands.Tests.ParameterParsingTests {
    [TestClass]
    public class SimpleVariableParameterProcessorTests {

        [TestMethod]
        public void ParseSimpleVector() {
            var command = ParseCommand("assign a to \"53573.9750085028:-26601.8512032533:12058.8229348438\"");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignCommand = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignCommand.variable is StaticVariable);
            StaticVariable variable = (StaticVariable)assignCommand.variable;
            Assert.IsTrue(variable.GetValue() is VectorPrimitive);
            Vector3D vector = CastVector(variable.GetValue()).GetVectorValue();
            Assert.AreEqual(53573.9750085028, vector.X);
            Assert.AreEqual(-26601.8512032533, vector.Y);
            Assert.AreEqual(12058.8229348438, vector.Z);
        }

        [TestMethod]
        public void ParseVectorFromGPSCoordinate() {
            var command = ParseCommand("assign a to \"GPS:surface:53573.9750085028:-26601.8512032533:12058.8229348438:#FF75C9F1:\"");
            Assert.IsTrue(command is VariableAssignmentCommand);
            VariableAssignmentCommand assignCommand = (VariableAssignmentCommand)command;
            Assert.IsTrue(assignCommand.variable is StaticVariable);
            StaticVariable variable = (StaticVariable)assignCommand.variable;
            Assert.IsTrue(variable.GetValue() is VectorPrimitive);
            Vector3D vector = CastVector(variable.GetValue()).GetVectorValue();
            Assert.AreEqual(53573.9750085028, vector.X);
            Assert.AreEqual(-26601.8512032533, vector.Y);
            Assert.AreEqual(12058.8229348438, vector.Z);
        }

        [TestMethod]
        public void AssignVariableToSelectorProperty() {
            var command = ParseCommand("assign \"vector\" to avg \"Main Cockpit\" position");
            Assert.IsTrue(command is VariableAssignmentCommand);
        }
    }
}