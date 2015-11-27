using System.Linq;
using FonsDijkstra.CConst;
using Microsoft.CodeAnalysis;
using Xunit;

namespace CConst.Test2
{
    public class CConst2Test
    {
        [Fact]
        public void Pure_method_without_calls_gives_no_diagnostics()
        {
            var source = @"
                using FonsDijkstra.CConst;

                class C
                {
                    [Const]
                    int F()
                    {
                        return 0;
                    }
                }
            ";
            Assert.Empty(DiagnosticHelper.GetDiagnostics(new InvocationInConstMethodAnalyzer(), source));
        }

        [Fact]
        public void Pure_method_may_call_pure_method()
        {
            var source = @"
                using FonsDijkstra.CConst;

                class C
                {
                    [Const]
                    int F()
                    {
                        G();
                        return 0;
                    }

                    [Const]
                    void G()
                    {
                    }
                }
            ";

            Assert.Empty(DiagnosticHelper.GetDiagnostics(new InvocationInConstMethodAnalyzer(), source));
        }

        [Fact]
        public void Pure_method_may_not_call_impure_method()
        {
            var source = @"
                using FonsDijkstra.CConst;

                class C
                {
                    [Const]
                    int F()
                    {
                        G(); // reports diagnostic CConst2
                        return 0;
                    }

                    void G()
                    {
                    }
                }
            ";

            Assert.True(DiagnosticHelper.GetDiagnostics(new InvocationInConstMethodAnalyzer(), source).Single().Id == InvocationInConstMethodAnalyzer.DiagnosticId);
        }

        [Fact]
        public void Impure_method_may_call_impure_method()
        {
            var source = @"
                class C
                {
                    int F()
                    {
                        G();
                        return 0;
                    }

                    void G()
                    {
                    }
                }
            ";

            var diagnostics = DiagnosticHelper.GetDiagnostics(new InvocationInConstMethodAnalyzer(), source);
            Assert.Empty(diagnostics);
        }
    }
}
