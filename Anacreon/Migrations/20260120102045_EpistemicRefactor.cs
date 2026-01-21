using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Anacreon.Migrations
{
    /// <inheritdoc />
    public partial class EpistemicRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- CRIAÇÃO DAS TABELAS ---

            migrationBuilder.CreateTable(
                name: "CodeInsights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Language = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Context = table.Column<string>(type: "TEXT", nullable: false),
                    Observation = table.Column<string>(type: "TEXT", nullable: false),
                    Suggestion = table.Column<string>(type: "TEXT", nullable: true),
                    Justification = table.Column<string>(type: "TEXT", nullable: true),
                    TradeOffs = table.Column<string>(type: "TEXT", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CodeSnippet = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: true),
                    LineNumber = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeInsights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ValidationSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Query = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CandidatesPresented = table.Column<int>(type: "INTEGER", nullable: false),
                    FactsApproved = table.Column<int>(type: "INTEGER", nullable: false),
                    FactsRejected = table.Column<int>(type: "INTEGER", nullable: false),
                    FactsEdited = table.Column<int>(type: "INTEGER", nullable: false),
                    WasCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Facts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Relation = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Object = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Confidence = table.Column<double>(type: "REAL", nullable: false, defaultValue: 0.0),
                    Status = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    ApprovedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ValidatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeprecatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeprecationReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ValidationSessionId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Facts_ValidationSessions_ValidationSessionId",
                        column: x => x.ValidationSessionId,
                        principalTable: "ValidationSessions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FactConflicts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Subject = table.Column<string>(type: "TEXT", nullable: false),
                    Relation = table.Column<string>(type: "TEXT", nullable: false),
                    FactAId = table.Column<int>(type: "INTEGER", nullable: false),
                    FactBId = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfidenceDifference = table.Column<double>(type: "REAL", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsResolved = table.Column<bool>(type: "INTEGER", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Resolution = table.Column<int>(type: "INTEGER", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactConflicts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactConflicts_Facts_FactAId",
                        column: x => x.FactAId,
                        principalTable: "Facts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FactConflicts_Facts_FactBId",
                        column: x => x.FactBId,
                        principalTable: "Facts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FactHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FactId = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    PreviousSubject = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PreviousRelation = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PreviousObject = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PreviousConfidence = table.Column<double>(type: "REAL", nullable: false),
                    PreviousStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    NewSubject = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    NewRelation = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    NewObject = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    NewConfidence = table.Column<double>(type: "REAL", nullable: false),
                    NewStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    ChangedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ChangeType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactHistory_Facts_FactId",
                        column: x => x.FactId,
                        principalTable: "Facts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FactSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FactId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Identifier = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    URL = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    TrustWeight = table.Column<double>(type: "REAL", nullable: false, defaultValue: 0.5),
                    CollectedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RawContent = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactSources_Facts_FactId",
                        column: x => x.FactId,
                        principalTable: "Facts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // --- ÍNDICES ---

            migrationBuilder.CreateIndex(name: "IX_Conversations_Timestamp", table: "Conversations", column: "Timestamp");
            migrationBuilder.CreateIndex(name: "IX_CodeInsights_CreatedAt", table: "CodeInsights", column: "CreatedAt");
            migrationBuilder.CreateIndex(name: "IX_FactConflicts_FactAId", table: "FactConflicts", column: "FactAId");
            migrationBuilder.CreateIndex(name: "IX_FactHistory_FactId", table: "FactHistory", column: "FactId");
            migrationBuilder.CreateIndex(name: "IX_Facts_Subject_Relation", table: "Facts", columns: new[] { "Subject", "Relation" });
            migrationBuilder.CreateIndex(name: "IX_FactSources_FactId", table: "FactSources", column: "FactId");

            // --- MIGRAÇÃO DE DADOS ORIGINAL (RESTAURADA COM O CASE) ---

            migrationBuilder.Sql(@"
                -- 1. Migrar Instances para Facts (com lógica de Confidence)
                INSERT INTO Facts (Subject, Relation, Object, Confidence, Status, CreatedAt, ValidatedAt, ApprovedBy, Version)
                SELECT 
                    i.Name as Subject,
                    'é' as Relation,
                    c.Name as Object,
                    CASE 
                        WHEN i.Confidence >= 1.0 THEN 0.95
                        WHEN i.Confidence = 0.0 THEN 0.80
                        ELSE i.Confidence 
                    END as Confidence,
                    1 as Status,
                    i.CreatedAt,
                    i.CreatedAt as ValidatedAt,
                    'migration' as ApprovedBy,
                    1 as Version
                FROM Instances i
                INNER JOIN Concepts c ON i.ConceptId = c.Id;

                -- 2. Criar fontes para fatos migrados
                INSERT INTO FactSources (FactId, Type, Identifier, TrustWeight, CollectedAt)
                SELECT 
                    f.Id,
                    0 as Type,
                    'Migrated from legacy system',
                    0.8,
                    f.CreatedAt
                FROM Facts f
                WHERE f.ApprovedBy = 'migration';

                -- 3. Migrar InstanceProperties para Facts adicionais
                INSERT INTO Facts (Subject, Relation, Object, Confidence, Status, CreatedAt, ValidatedAt, ApprovedBy, Version)
                SELECT 
                    i.Name as Subject,
                    ip.PropertyName as Relation,
                    ip.PropertyValue as Object,
                    0.90 as Confidence,
                    1 as Status,
                    ip.CreatedAt,
                    ip.CreatedAt,
                    'migration' as ApprovedBy,
                    1 as Version
                FROM InstanceProperties ip
                INNER JOIN Instances i ON ip.InstanceId = i.Id;

                -- 4. Criar histórico inicial completo
                INSERT INTO FactHistory (FactId, Version, NewSubject, NewRelation, NewObject, NewConfidence, NewStatus, ChangedBy, ChangedAt, Reason, ChangeType)
                SELECT 
                    Id,
                    1,
                    Subject,
                    Relation,
                    Object,
                    Confidence,
                    Status,
                    'migration',
                    CreatedAt,
                    'Initial migration from legacy system',
                    0
                FROM Facts
                WHERE ApprovedBy = 'migration';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "FactSources");
            migrationBuilder.DropTable(name: "FactHistory");
            migrationBuilder.DropTable(name: "FactConflicts");
            migrationBuilder.DropTable(name: "Facts");
            migrationBuilder.DropTable(name: "ValidationSessions");
            migrationBuilder.DropTable(name: "CodeInsights");
            migrationBuilder.DropIndex(name: "IX_Conversations_Timestamp", table: "Conversations");
        }
    }
}