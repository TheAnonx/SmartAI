using System;
using System.Collections.Generic;

namespace SmartAI.Cognitive
{
    /// <summary>
    /// Define permissões por modo cognitivo.
    /// ESTAS REGRAS SÃO INVIOLÁVEIS.
    /// </summary>
    public static class CognitivePermissions
    {
        private static readonly Dictionary<CognitiveMode, ModePermissions> _permissions = new()
        {
            [CognitiveMode.ANSWER] = new ModePermissions
            {
                CanSearchWeb = false,
                CanLearn = false,
                CanAssert = true,
                RequiresHighConfidence = true
            },
            [CognitiveMode.INVESTIGATION] = new ModePermissions
            {
                CanSearchWeb = true,
                CanLearn = false,
                CanAssert = false,
                RequiresHighConfidence = false
            },
            [CognitiveMode.VALIDATION] = new ModePermissions
            {
                CanSearchWeb = false,
                CanLearn = false,
                CanAssert = false,
                RequiresHighConfidence = false
            },
            [CognitiveMode.LEARNING] = new ModePermissions
            {
                CanSearchWeb = false,
                CanLearn = true,
                CanAssert = false,
                RequiresHighConfidence = false
            },
            [CognitiveMode.CODE_ANALYSIS] = new ModePermissions
            {
                CanSearchWeb = true,
                CanLearn = false,
                CanAssert = false,
                RequiresHighConfidence = false
            }
        };

        public static ModePermissions GetPermissions(CognitiveMode mode)
        {
            return _permissions[mode];
        }

        public static void ValidateAction(CognitiveMode mode, CognitiveAction action)
        {
            var permissions = GetPermissions(mode);

            switch (action)
            {
                case CognitiveAction.SearchWeb when !permissions.CanSearchWeb:
                    throw new InvalidOperationException(
                        $"Mode {mode} cannot search web");

                case CognitiveAction.Learn when !permissions.CanLearn:
                    throw new InvalidOperationException(
                        $"Mode {mode} cannot learn");

                case CognitiveAction.Assert when !permissions.CanAssert:
                    throw new InvalidOperationException(
                        $"Mode {mode} cannot assert facts");

                default:
                    // Ação permitida
                    break;
            }
        }
    }

    public class ModePermissions
    {
        public bool CanSearchWeb { get; set; }
        public bool CanLearn { get; set; }
        public bool CanAssert { get; set; }
        public bool RequiresHighConfidence { get; set; }
    }

    public enum CognitiveAction
    {
        SearchWeb,
        Learn,
        Assert,
        Validate
    }
}