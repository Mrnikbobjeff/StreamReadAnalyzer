using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace StreamNoDiscardAnalyzer.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {
        [TestMethod]
        public void EmptyTextNoDiagnostic()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void ReadCall_TriggersDiagnostic()
        {
            var test = @"
    using System;
    using System.IO;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public TypeName()
            {
                var ms = new MemoryStream();
                ms.Read(new byte[1],0,1);
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "StreamNoDiscardAnalyzer",
                Message = "Stream read call should not discard return value of actual bytes read",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 12, 17)
                        }
            };
            VerifyCSharpDiagnostic(test, expected);
        }


        [TestMethod]
        public void ReadCall_TriggersDiagnostic_TestsFix()
        {
            var test = @"
    using System;
    using System.IO;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public TypeName()
            {
                var ms = new MemoryStream();
                ms.Read(new byte[1],0,1);
            }
        }
    }";
            var expected = @"
    using System;
    using System.IO;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public TypeName()
            {
                var ms = new MemoryStream();
            var readCount = ms.Read(new byte[1],0,1);
        }
        }
    }";
            VerifyCSharpFix(test, expected);
        }

        [TestMethod]
        public void ReadCall_TriggersNoDiagnostic()
        {
            var test = @"
    using System;
    using System.IO;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public TypeName()
            {
                var ms = new MemoryStream();
                var yx = ms.Read(new byte[1],0,1);
            }
        }
    }";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void ReadCall_TriggersNoDiagnostic_Comparison()
        {
            var test = @"
    using System;
    using System.IO;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public TypeName()
            {
                var ms = new MemoryStream();
                if(x > ms.Read(new byte[1],0,1))
                    return;
            }
        }
    }";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void ReadCall_TriggersNoDiagnostic_CastIsCompensatedFor()
        {
            var test = @"
    using System;
    using System.IO;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public TypeName()
            {
                var ms = new MemoryStream();
                var yx = (long) ms.Read(new byte[1],0,1);
            }
        }
    }";
            VerifyCSharpDiagnostic(test);
        }


        [TestMethod]
        public void ReadCall_TriggersNoDiagnostic_FakeStream()
        {
            var test = @"
    using System;
    using System.IO;

    namespace ConsoleApplication1
    {
        public class FakeStream {public int Read() => 1;}
        class TypeName
        {
            public TypeName()
            {
                var ms = new FakeStream();
                ms.Read();
            }
        }
    }";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void ReadCall_TriggersNoDiagnostic_FakeStream_NoReadCall()
        {
            var test = @"
    using System;
    using System.IO;

    namespace ConsoleApplication1
    {
        public class FakeStream {public int FakeRead() => 1;}
        class TypeName
        {
            public TypeName()
            {
                var ms = new FakeStream();
                ms.FakeRead();
            }
        }
    }";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void ReadCall_TriggersNoDiagnostic_FakeStream_DoesNotReturnReadLength()
        {
            var test = @"
    using System;
    using System.IO;

    namespace ConsoleApplication1
    {
        public class FakeStream {public System.Void Read() {}}
        class TypeName
        {
            public TypeName()
            {
                var ms = new FakeStream();
                ms.Read();
            }
        }
    }";
            VerifyCSharpDiagnostic(test);
        }


        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new StreamNoDiscardAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new StreamNoDiscardAnalyzerAnalyzer();
        }
    }
}
