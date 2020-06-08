﻿using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {
        public static class ParameterProcessorRegistry {
            private static List<ParameterProcessor> parameterProcessors = new List<ParameterProcessor>
            {
                  new SelectorProcessor(),
                  new FunctionProcessor(),
                  new RunArgumentProcessor(),
                  new IterationProcessor(),
                  new ConditionProcessor(),
                  new ActionProcessor(),
            };

            public static void process(List<CommandParameter> commandParameters) {
                foreach (ParameterProcessor parser in parameterProcessors) {
                    Debug("Start Parameter Processor: " + parser.GetType());
                    Debug("Pre Processed Parameters:");
                    commandParameters.ForEach(param => Debug("Type: " + param.GetType()));
                    parser.Process(commandParameters);
                    Debug("End Parameter Processor: " + parser.GetType());
                    Debug("Post Processed Parameters:");
                    commandParameters.ForEach(param => Debug("Type: " + param.GetType()));
                }
            }
        }

        public abstract class ParameterProcessor {
            protected abstract bool ShouldProcess(CommandParameter p);
            protected abstract void ConvertNext(List<CommandParameter> p, ref int i);
            public void Process(List<CommandParameter> p) { for (int i = 0; i < p.Count; i++) { if (ShouldProcess(p[i])) ConvertNext(p, ref i); } }
        }

        public class SelectorProcessor : ParameterProcessor {
            protected override bool ShouldProcess(CommandParameter p) {
                return p is GroupCommandParameter || p is StringCommandParameter || p is BlockTypeCommandParameter || p is IndexCommandParameter;
            }
            protected override void ConvertNext(List<CommandParameter> p, ref int i) {
                bool isGroup = false;
                StringCommandParameter selector = null;
                BlockTypeCommandParameter blockType = null;
                int? index = null;
                int paramCount = 0;

                while (i + paramCount < p.Count) {
                    CommandParameter param = p[i + paramCount];

                    if (param is GroupCommandParameter) isGroup = true;
                    else if (param is StringCommandParameter && selector == null) selector = ((StringCommandParameter)param);
                    else if (param is BlockTypeCommandParameter && blockType == null) blockType = ((BlockTypeCommandParameter)param);
                    else if (param is IndexCommandParameter) index = ((IndexCommandParameter)param).Value;
                    else break;
                    paramCount++;
                }

                if (selector == null) throw new Exception("All selectors must have a string identifier");

                if (blockType == null) {
                    int blockTypeIndex = selector.SubTokens.FindLastIndex(s => s is BlockTypeCommandParameter);//Use Last Block Type
                    if (blockTypeIndex < 0) return; //Apparently not a Selector
                    if (selector.SubTokens.Exists(s => s is GroupCommandParameter)) isGroup = true;
                    blockType = ((BlockTypeCommandParameter)selector.SubTokens[blockTypeIndex]);
                }

                Debug("Converted String at index: " + i + " to SelectorCommandParamter");
                p.RemoveRange(i, paramCount);
                p.Insert(i, new SelectorCommandParameter(blockType.GetBlockType(), isGroup, selector.Value, index));
            }
        }

        public class FunctionProcessor : ParameterProcessor {
            protected override bool ShouldProcess(CommandParameter p) { return p is StringCommandParameter; }
            protected override void ConvertNext(List<CommandParameter> p, ref int i) {
                StringCommandParameter param = (StringCommandParameter)p[i];
                if (FUNCTIONS.ContainsKey(param.Value)) { p.Insert(i, new FunctionCommandParameter(FunctionType.GOTO)); i++; }
            }
        }
        public class RunArgumentProcessor : ParameterProcessor {
            protected override bool ShouldProcess(CommandParameter p) { return p is StringCommandParameter; }
            protected override void ConvertNext(List<CommandParameter> p, ref int i) {
                StringCommandParameter param = (StringCommandParameter)p[i];
                if (param.SubTokens.Count == 0 || !(param.SubTokens[0] is StringPropertyCommandParameter)) return;
                if (((StringPropertyCommandParameter)param.SubTokens[0]).Value != StringPropertyType.RUN) return;
                Debug("Found Run Keyword!");
                List<Token> values = ParseTokens(param.Value);
                p.RemoveAt(i);
                values.RemoveAt(0);
                Debug("Arguments: (" + String.Join(" ", values) + ")");
                p.Insert(i, new StringPropertyCommandParameter(StringPropertyType.RUN));
                p.Insert(i + 1, new StringCommandParameter(String.Join(" ", values)));
            }
        }

        public class ActionProcessor : ParameterProcessor {
            protected override bool ShouldProcess(CommandParameter p) {
                return p is SelectorCommandParameter ||
                    p is DirectionCommandParameter ||
                    p is NumericCommandParameter ||
                    p is StringCommandParameter ||
                    p is BooleanCommandParameter ||
                    p is NumericPropertyCommandParameter ||
                    p is BooleanPropertyCommandParameter ||
                    p is StringPropertyCommandParameter ||
                    p is ReverseCommandParameter ||
                    p is RelativeCommandParameter ||
                    p is WaitCommandParameter ||
                    p is UnitCommandParameter ||
                    p is ControlCommandParameter ||
                    p is FunctionCommandParameter ||
                    p is ListenCommandParameter ||
                    p is SendCommandParameter;
            }
            protected override void ConvertNext(List<CommandParameter> p, ref int i) {
                int paramCount = 0;
                while (i + paramCount < p.Count) {
                    if (!ShouldProcess(p[i + paramCount])) break;
                    paramCount++;
                }
                List<CommandParameter> action = p.GetRange(i, paramCount);

                Command command;
                if (action.Exists(a => a is FunctionCommandParameter)) command = new FunctionCommand(action);
                else if (action.Exists(a => a is ListenCommandParameter)) command = new ListenCommand(action);
                else if (action.Exists(a => a is SendCommandParameter)) command = new SendCommand(action);
                else if (action.Exists(a => a is ControlCommandParameter)) command = new ControlCommand(action);
                else if (action.Exists(a => a is WaitCommandParameter)) command = new WaitCommand(action);
                else if (action.Exists(a => a is SelectorCommandParameter)) command = new BlockCommand(action);
                else throw new Exception("Unknown Command Reference Type.");
                p.RemoveRange(i, paramCount);
                p.Insert(i, new CommandReferenceParameter(command));
            }
        }

        public class IterationProcessor : ParameterProcessor {
            protected override void ConvertNext(List<CommandParameter> p, ref int i) {
                IterationCommandParameter icp = (IterationCommandParameter)p[i];
                if (i == 0 || !(p[i - 1] is NumericCommandParameter)) throw new Exception("Iteration must be preceded by a number");
                icp.Value = (int)Math.Round(((NumericCommandParameter)p[i - 1]).Value);
                Print("Loops: " + icp.Value);
                p.RemoveAt(i - 1);
            }

            protected override bool ShouldProcess(CommandParameter p) {
                return p is IterationCommandParameter;
            }
        }

        public class ConditionProcessor : ParameterProcessor {
            protected override bool ShouldProcess(CommandParameter p) { return p is IfCommandParameter; }
            protected override void ConvertNext(List<CommandParameter> p, ref int i) {
                Debug("Attempting to parse Condition at index " + i);
                parseNextConditionTokens(p, i + 1);
                resolveNextCondition(p, i + 1);
            }

            public void parseNextConditionTokens(List<CommandParameter> commandParameters, int index) {
                Debug("Attempting to parse Condition Tokens at index " + index);
                SelectorCommandParameter selector = null;//required
                ComparisonCommandParameter comparator = null;//Can be defaulted
                PrimitiveCommandParameter value = null;//requred? One of primitive or property must be set.
                PropertyCommandParameter property = null;//Can be defaulted
                AggregationModeCommandParameter aggregation = null;
                bool inverseAggregation = false;
                bool inverseBlockCondition = false;

                if (commandParameters[index] is NotCommandParameter || commandParameters[index] is OpenParenthesisCommandParameter) {
                    Debug("Token is " + commandParameters[index].GetType() + ", continuing");
                    parseNextConditionTokens(commandParameters, index + 1);
                    return;
                }

                int paramCount = 0;
                while (index + paramCount < commandParameters.Count) {
                    CommandParameter param = commandParameters[index + paramCount];

                    if (param is SelectorCommandParameter && selector == null) selector = (SelectorCommandParameter)param;
                    else if (param is ComparisonCommandParameter && comparator == null) comparator = (ComparisonCommandParameter)param;
                    else if (param is AggregationModeCommandParameter && aggregation == null) aggregation = (AggregationModeCommandParameter)param;
                    else if (param is PropertyCommandParameter && property == null) property = (PropertyCommandParameter)param;
                    else if (param is PrimitiveCommandParameter && value == null) value = ((PrimitiveCommandParameter)param);
                    else if (param is NotCommandParameter) {
                        if (aggregation != null || selector != null) {
                            inverseBlockCondition = !inverseBlockCondition;
                        } else {
                            inverseAggregation = !inverseAggregation;
                        }
                    } else break;
                    paramCount++;
                }

                if (selector == null) throw new Exception("All conditions must have a selector");
                if (value == null && property == null) throw new Exception("All conditions must have either a property or a value");

                Debug("Finished parsing condition params.  Count: " + paramCount);
                AggregationMode aggregationMode = (aggregation == null) ? AggregationMode.ALL : aggregation.Value;
                ComparisonType comparison = (comparator == null) ? ComparisonType.EQUAL : comparator.Value;
                BlockHandler handler = BlockHandlerRegistry.GetBlockHandler(selector.blockType);
                SelectorEntityProvider provider = new SelectorEntityProvider(selector);

                BlockCondition blockCondition;
                if (value is BooleanCommandParameter || property is BooleanPropertyCommandParameter) {
                    Debug("Boolean Command");
                    BooleanPropertyType boolProperty = handler.GetDefaultBooleanProperty();
                    if (property != null) boolProperty = ((BooleanPropertyCommandParameter)property).Value;
                    bool boolValue = true; if (value != null) boolValue = ((BooleanCommandParameter)value).Value;
                    blockCondition = new BooleanBlockCondition(handler, boolProperty, new BooleanComparator(comparison), boolValue);
                } else if (value is StringCommandParameter || property is StringPropertyCommandParameter) {
                    Debug("String Command");
                    StringPropertyType stringProperty = handler.GetDefaultStringProperty();
                    if (property != null) stringProperty = ((StringPropertyCommandParameter)property).Value;
                    if (value == null) throw new Exception("String Comparison Value Cannot Be Left Blank");
                    String stringValue = ((StringCommandParameter)value).Value;
                    blockCondition = new StringBlockCondition(handler, stringProperty, new StringComparator(comparison), stringValue);
                } else if (value is NumericCommandParameter || property is NumericPropertyCommandParameter) {
                    Debug("Numeric Command");
                    NumericPropertyType numericProperty = handler.GetDefaultNumericProperty(handler.GetDefaultDirection());
                    if (property != null) numericProperty = ((NumericPropertyCommandParameter)property).Value;
                    if (value == null) throw new Exception("Numeric Comparison Value Cannot Be Left Blank");
                    float numericValue = ((NumericCommandParameter)value).Value;
                    blockCondition = new NumericBlockCondition(handler, numericProperty, new NumericComparator(comparison), numericValue);
                } else {
                    throw new Exception("Unsupported Condition Parameters");
                }

                Debug("Inverse Block Condition: " + inverseBlockCondition);
                Debug("Inverse Aggregation: " + inverseAggregation);

                if (inverseBlockCondition) blockCondition = new NotBlockCondition(blockCondition);
                Condition condition = new AggregateCondition(aggregationMode, blockCondition, new SelectorEntityProvider(selector));
                if (inverseAggregation) condition = new NotCondition(condition);

                Debug("Removing Range: " + index + ", Params: " + paramCount);
                commandParameters.RemoveRange(index, paramCount);
                commandParameters.Insert(index, new ConditionCommandParameter(condition));

                if (commandParameters.Count == index + 1) return;

                //TODO: There's definitely bugs with this..
                int newIndex = index + 1;
                while (commandParameters[newIndex] is CloseParenthesisCommandParameter) { newIndex++; }
                if (commandParameters.Count == newIndex + 1) return;
                if (commandParameters[newIndex] is AndCommandParameter || commandParameters[newIndex] is OrCommandParameter) {
                    Debug("Next Token After Processing Condition is " + commandParameters[newIndex].GetType() + ", continuing");
                    parseNextConditionTokens(commandParameters, newIndex + 1);
                }
                Debug("Finished Processing Condition Tokens at index: " + index);
            }

            public void resolveNextCondition(List<CommandParameter> commandParameters, int index) {
                Debug("Attempting to resolve Condition at index " + index);
                if (commandParameters[index] is NotCommandParameter) // Handle Nots first
                {
                    commandParameters.RemoveAt(index); // Remove Not
                    Condition notCondition = new NotCondition(getNextCondition(commandParameters, index));
                    commandParameters.RemoveAt(index);
                    commandParameters.Insert(index, new ConditionCommandParameter(notCondition));
                } else if (commandParameters[index] is OpenParenthesisCommandParameter) { //Handle Parenthesis Next
                    resolveNextCondition(commandParameters, index + 1);
                    if (!(commandParameters[index + 2] is CloseParenthesisCommandParameter)) throw new Exception("Mismatched Parenthesis!");
                    commandParameters.RemoveAt(index); //Remove Open Parenthesis
                    commandParameters.RemoveAt(index + 1); //Remove Close Parenthesis
                }

                if (!(commandParameters[index] is ConditionCommandParameter)) throw new Exception("Invalid Token Inside Condition: " + commandParameters[index].GetType());
                Condition conditionA = ((ConditionCommandParameter)commandParameters[index]).Value;
                while (commandParameters.Count > index + 2) //Look for And/Or + more conditions
                {
                    if (commandParameters[index + 1] is AndCommandParameter) {//Handle Ands before Ors
                        Debug("Found And Parameter at index: " + (index + 1));
                        AndCondition andCondition = new AndCondition(conditionA, getNextCondition(commandParameters, index + 2));
                        commandParameters.RemoveRange(index, 3);
                        commandParameters.Insert(index, new ConditionCommandParameter(andCondition));
                    } else if (commandParameters[index + 1] is OrCommandParameter) {
                        Debug("Found Or Parameter at index: " + (index + 1));
                        resolveNextCondition(commandParameters, index + 2);
                        OrCondition orCondition = new OrCondition(conditionA, getNextCondition(commandParameters, index + 2));
                        commandParameters.RemoveRange(index, 3);
                        commandParameters.Insert(index, new ConditionCommandParameter(orCondition));
                    } else {
                        break;
                    }
                }
            }

            private Condition getNextCondition(List<CommandParameter> commandParameters, int index) {
                if (!(commandParameters[index] is ConditionCommandParameter)) ///Resolve if not a simple condition (more parentheses, for example)
                {
                    Debug("In getNextCondition.  Next Condition at index " + index + " is not simple, resolving");
                    resolveNextCondition(commandParameters, index);
                }
                return ((ConditionCommandParameter)commandParameters[index]).Value;
            }
        }
    }
}
