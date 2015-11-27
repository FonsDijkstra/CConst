using System.Linq;
using FonsDijkstra.CConst;
using Microsoft.CodeAnalysis;
using Xunit;

namespace CConst.Test2
{
    public class CConst1Test
    {
        [Fact]
        public void Pure_method_without_assignments_gives_no_disagnostics()
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
            var diagnostics = DiagnosticHelper.GetDiagnostics(new AssignmentInConstMethodAnalyzer(), source);
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void Pure_method_may_assign_to_local_variable()
        {
            var source = @"
                using FonsDijkstra.CConst;

                class C
                {
                    [Const]
                    int F()
                    {
                        int i = 0;
                        return i;
                    }
                }
            ";

            Assert.Empty(DiagnosticHelper.GetDiagnostics(new AssignmentInConstMethodAnalyzer(), source));
        }

        [Fact]
        public void Pure_method_may_not_assign_field()
        {
            var source = @"
                using FonsDijkstra.CConst;

                class C
                {
                    int i;

                    [Const]
                    int F()
                    {
                        i = 2;
                        return i;
                    }
                }
            ";

            Assert.True(DiagnosticHelper.GetDiagnostics(new AssignmentInConstMethodAnalyzer(), source).Single().Id == AssignmentInConstMethodAnalyzer.DiagnosticId);
        }

        [Fact]
        public void Impure_method_may_assign_field()
        {
            var source = @"
                class C
                {
                    int i;

                    int F()
                    {
                        i = 2;
                        return i;
                    }
                }
            ";

            Assert.Empty(DiagnosticHelper.GetDiagnostics(new AssignmentInConstMethodAnalyzer(), source));
        }
    }
}
