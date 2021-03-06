﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Persimmon.TestRunner.Internals
{
    public sealed class RunSinkTrampoline :
        MarshalByRefObject,
        ISinkTrampoline,
        IConvertible
    {
        private readonly string targetAssemblyPath_;
        private readonly ITestRunSink parentSink_;
        private readonly Dictionary<string, TestCase> testCases_;

        internal RunSinkTrampoline(
            string targetAssemblyPath,
            ITestRunSink parentSink,
            Dictionary<string, TestCase> testCases)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(targetAssemblyPath));
            Debug.Assert(parentSink != null);
            Debug.Assert(testCases != null);

            targetAssemblyPath_ = targetAssemblyPath;
            parentSink_ = parentSink;
            testCases_ = testCases;
        }

        public void Begin(string message)
        {
            parentSink_.Begin(message);
        }

        public void Message(bool isError, string message)
        {
            parentSink_.Message(isError, message);
        }

        public void Progress(dynamic[] args)
        {
            string fullyQualifiedTestName = args[0];
            string symbolName = args[1];
            string displayName = args[2];
            Exception[] exceptions = (args[3] as dynamic[]).Select(c => c.Unwrap as Exception).ToArray();
            string[] skips = args[4];
            string[] failures = args[5];
            TimeSpan duration = args[6];

            TestCase testCase;
            if (testCases_.TryGetValue(fullyQualifiedTestName, out testCase) == false)
            {
                Debug.WriteLine(string.Format(
                    "TestCase lookup failed: FQTN=\"{0}\", SymbolName=\"{1}\", DisplayName=\"{2}\"",
                    fullyQualifiedTestName,
                    symbolName,
                    displayName));

                // Invalid fqtn, try create only basic informational TestCase...
                //Debug.Fail("Not valid fqtn: " + fqtn);
                testCase = new TestCase(
                    fullyQualifiedTestName,
                    parentSink_.ExtensionUri,
                    targetAssemblyPath_);
                testCase.DisplayName = displayName;
            }

            var testResult = new TestResult(testCase);

            // TODO: Other outcome require handled.
            //   Strategy: testCases_ included target test cases,
            //     so match and filter into Finished(), filtered test cases marking TestOutcome.Notfound.
            if (exceptions.Length >= 1)
            {
                testResult.Outcome = TestOutcome.Failed;
                testResult.ErrorMessage = exceptions[0].Message;
                testResult.ErrorStackTrace = exceptions[0].StackTrace;
            }
            else if (skips.Length >= 1)
            {
                testResult.Outcome = TestOutcome.Skipped;
                foreach (var msg in skips) testResult.Messages.Add(new TestResultMessage("", msg));
            }
            else if (failures.Length >= 1)
            {
                testResult.Outcome = TestOutcome.Failed;
                foreach (var msg in failures) testResult.Messages.Add(new TestResultMessage("", msg));
            }
            else testResult.Outcome = TestOutcome.Passed;
            testResult.Duration = duration;

            parentSink_.Progress(testResult);
        }

        public void Finished(string message)
        {
            parentSink_.Finished(message);
        }

        public TypeCode GetTypeCode()
        {
            return TypeCode.Object;
        }

        public bool ToBoolean(IFormatProvider provider)
        {
            return ThrowInvalidCast<bool>();
        }

        public char ToChar(IFormatProvider provider)
        {
            return ThrowInvalidCast<char>();
        }

        public sbyte ToSByte(IFormatProvider provider)
        {
            return ThrowInvalidCast<sbyte>();
        }

        public byte ToByte(IFormatProvider provider)
        {
            return ThrowInvalidCast<byte>();
        }

        public short ToInt16(IFormatProvider provider)
        {
            return ThrowInvalidCast<short>();
        }

        public ushort ToUInt16(IFormatProvider provider)
        {
            return ThrowInvalidCast<ushort>();
        }

        public int ToInt32(IFormatProvider provider)
        {
            return ThrowInvalidCast<int>();
        }

        public uint ToUInt32(IFormatProvider provider)
        {
            return ThrowInvalidCast<uint>();
        }

        public long ToInt64(IFormatProvider provider)
        {
            return ThrowInvalidCast<long>();
        }

        public ulong ToUInt64(IFormatProvider provider)
        {
            return ThrowInvalidCast<ulong>();
        }

        public float ToSingle(IFormatProvider provider)
        {
            return ThrowInvalidCast<float>();
        }

        public double ToDouble(IFormatProvider provider)
        {
            return ThrowInvalidCast<double>();
        }

        public decimal ToDecimal(IFormatProvider provider)
        {
            return ThrowInvalidCast<decimal>();
        }

        public DateTime ToDateTime(IFormatProvider provider)
        {
            return ThrowInvalidCast<DateTime>();
        }

        public string ToString(IFormatProvider provider)
        {
            return ThrowInvalidCast<string>();
        }

        public object ToType(Type conversionType, IFormatProvider provider)
        {
            if (conversionType == typeof(ISinkTrampoline) || conversionType == typeof(RunSinkTrampoline))
            {
                return this;
            }
            return ThrowInvalidCast(conversionType);
        }

        private T ThrowInvalidCast<T>()
        {
            return (T)ThrowInvalidCast(typeof(T));
        }

        private object ThrowInvalidCast(Type type)
        {
            throw new InvalidCastException($"RunSinkTrampoline does not cast {type.FullName}.");
        }
    }
}
