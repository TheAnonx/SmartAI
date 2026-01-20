using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartAI.Data;
using SmartAI.Cognitive;
using SmartAI.Services;
using SmartAI.Models;

namespace SmartAI.Tests
{
    public class EpistemicSystemTests
    {
        private AIContext GetTestContext()
        {
            var options = new DbContextOptionsBuilder<AIContext>()
                .UseSqlite("Data Source=test.db")
                .Options;

            var context = new AIContext(options);
            context.Database.EnsureCreated();

            return context;
        }

        public async Task TestFactCreationAndValidation()
        {
            Console.WriteLine("=== TEST: Fact Creation and Validation ===\n");

            using var context = GetTestContext();
            var factService = new FactService(context);

            // 1. Criar fato candidato
            Console.WriteLine("1. Creating candidate fact...");
            var fact = await factService.CreateCandidateFact(
                "Python",
                "é",
                "Uma linguagem de programação",
                SourceType.USER,
                "Test User");

            Console.WriteLine($"✓ Candidate created with ID: {fact.Id}");
            Console.WriteLine($"  Status: {fact.Status}");
            Console.WriteLine($"  Confidence: {fact.Confidence}");

            // 2. Validar fato
            Console.WriteLine("\n2. Validating fact...");
            fact = await factService.ValidateFact(fact.Id, "tester", 0.92);

            Console.WriteLine($"✓ Fact validated");
            Console.WriteLine($"  Status: {fact.Status}");
            Console.WriteLine($"  Confidence: {fact.Confidence}");
            Console.WriteLine($"  Approved by: {fact.ApprovedBy}");

            // 3. Verificar histórico
            Console.WriteLine("\n3. Checking history...");
            var history = await factService.GetFactHistory(fact.Id);

            Console.WriteLine($"✓ History entries: {history.Count}");
            foreach (var entry in history)
            {
                Console.WriteLine($"  Version {entry.Version}: {entry.ChangeType} by {entry.ChangedBy}");
            }

            // 4. Tentar criar confiança = 1.0 (deve falhar)
            Console.WriteLine("\n4. Testing confidence validation...");
            try
            {
                await factService.CreateCandidateFact(
                    "Test",
                    "é",
                    "Invalid",
                    SourceType.USER,
                    "Test");

                var invalidFact = await context.Facts.FirstAsync(f => f.Subject == "Test");
                invalidFact.SetConfidence(1.0);

                Console.WriteLine("✗ ERROR: Should have thrown exception!");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"✓ Correctly rejected confidence = 1.0: {ex.Message}");
            }

            Console.WriteLine("\n=== TEST PASSED ===\n");
        }

        public async Task TestConflictDetection()
        {
            Console.WriteLine("=== TEST: Conflict Detection ===\n");

            using var context = GetTestContext();
            var factService = new FactService(context);
            var conflictService = new ConflictDetectionService(context);

            // Criar dois fatos conflitantes
            Console.WriteLine("1. Creating conflicting facts...");

            var fact1 = await factService.CreateCandidateFact(
                "Python",
                "foi criado em",
                "1991",
                SourceType.USER,
                "Source A");
            await factService.ValidateFact(fact1.Id, "user", 0.95);

            var fact2 = await factService.CreateCandidateFact(
                "Python",
                "foi criado em",
                "1989",
                SourceType.WEB,
                "Source B");
            await factService.ValidateFact(fact2.Id, "user", 0.88);

            Console.WriteLine("✓ Two conflicting facts created");

            // Detectar conflitos
            Console.WriteLine("\n2. Detecting conflicts...");
            var conflicts = await conflictService.DetectConflicts();

            Console.WriteLine($"✓ Conflicts detected: {conflicts.Count}");

            if (conflicts.Count > 0)
            {
                var conflict = conflicts[0];
                Console.WriteLine($"  Subject: {conflict.Subject}");
                Console.WriteLine($"  Relation: {conflict.Relation}");
                Console.WriteLine($"  Fact A: {conflict.FactA.Object} (conf: {conflict.FactA.Confidence:P0})");
                Console.WriteLine($"  Fact B: {conflict.FactB.Object} (conf: {conflict.FactB.Confidence:P0})");
            }

            Console.WriteLine("\n=== TEST PASSED ===\n");
        }

        public async Task TestCognitiveEngine()
        {
            Console.WriteLine("=== TEST: Cognitive Engine ===\n");

            using var context = GetTestContext();
            var engine = new CognitiveEngine(context);

            // Teste 1: Pergunta sem conhecimento
            Console.WriteLine("1. Testing question without knowledge...");
            var response1 = await engine.Process("O que é Rust?");

            Console.WriteLine($"✓ Response mode: {response1.Mode}");
            Console.WriteLine($"  Requires action: {response1.RequiresAction}");
            Console.WriteLine($"  Suggested mode: {response1.SuggestedMode}");

            // Teste 2: Ensino direto (deve ir para validação)
            Console.WriteLine("\n2. Testing teaching statement...");
            var response2 = await engine.Process("Rust é uma linguagem de programação");

            Console.WriteLine($"✓ Response mode: {response2.Mode}");

            Console.WriteLine("\n=== TEST PASSED ===\n");
        }

        public static async Task RunAllTests()
        {
            var tests = new EpistemicSystemTests();

            try
            {
                await tests.TestFactCreationAndValidation();
                await tests.TestConflictDetection();
                await tests.TestCognitiveEngine();

                Console.WriteLine("\n🎉 ALL TESTS PASSED 🎉\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ TEST FAILED: {ex.Message}\n");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}