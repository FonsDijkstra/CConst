using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FonsDijkstra.CConst;
using Xunit;

namespace CConst.Test2
{
    public class CConst52Test
    {
        [Fact]
        public void Interface_implementation_of_pure_method_may_not_be_impure()
        {
            var source = @"
                using FonsDijkstra.CConst;

                interface I
                {
                    [Const]
                    bool F();
                }

                class B : I
                {
                    public bool F() // reports diagnostic CConst51
                    {
                        return false;
                    }
                }
            ";
            var diagnostics = DiagnosticHelper.GetDiagnostics(new ConstPolymorphismAnalyzer(), source);
            Assert.True(diagnostics.Single().Id == ConstPolymorphismAnalyzer.InterfaceDiagnosticId);
        }

        [Fact]
        public void Interface_implementation_of_pure_method_must_be_pure()
        {
            var source = @"
                using FonsDijkstra.CConst;

                interface I
                {
                    [Const]
                    bool F();
                }

                class B : I
                {
                    [Const]
                    public bool F()
                    {
                        return false;
                    }
                }
            ";

            Assert.Empty(DiagnosticHelper.GetDiagnostics(new ConstPolymorphismAnalyzer(), source));
        }

        [Fact]
        public void Interface_implementation_of_impure_method_may_be_pure()
        {
            var source = @"
                using FonsDijkstra.CConst;

                interface I
                {
                    bool F();
                }

                class B : I
                {
                    [Const]
                    public bool F()
                    {
                        return false;
                    }
                }
            ";

            Assert.Empty(DiagnosticHelper.GetDiagnostics(new ConstPolymorphismAnalyzer(), source));
        }

        [Fact]
        public void Interface_implementation_of_impure_method_may_be_impure()
        {
            var source = @"
                interface I
                {
                    bool F();
                }

                class B : I
                {
                    public bool F()
                    {
                        return false;
                    }
                }
            ";

            Assert.Empty(DiagnosticHelper.GetDiagnostics(new ConstPolymorphismAnalyzer(), source));
        }
    }
}
