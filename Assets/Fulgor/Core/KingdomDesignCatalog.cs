using System;
using System.Collections.Generic;
using System.Linq;

namespace Fulgor.Core.Campaign
{
    /// <summary>Proposed faction identity data. These descriptions do not apply gameplay bonuses.</summary>
    public sealed class KingdomDesign
    {
        internal KingdomDesign(string id, string name, string primary, string secondary, string accent,
            string building, string specialUnit, string eliteUnit, string advantage, string limitation, bool candidate)
        {
            Id = id; Name = name; PrimaryColor = primary; SecondaryColor = secondary; AccentColor = accent;
            Building = building; SpecialUnit = specialUnit; EliteUnit = eliteUnit;
            Advantage = advantage; Limitation = limitation; IsCandidate = candidate;
        }

        public string Id { get; }
        public string Name { get; }
        public string PrimaryColor { get; }
        public string SecondaryColor { get; }
        public string AccentColor { get; }
        public string Building { get; }
        public string SpecialUnit { get; }
        public string EliteUnit { get; }
        public string Advantage { get; }
        public string Limitation { get; }
        public bool IsCandidate { get; }
    }

    public static class KingdomDesignCatalog
    {
        public static IReadOnlyList<KingdomDesign> All { get; } = Array.AsReadOnly(new[] {
            new KingdomDesign("asturias", "Asturias", "24518A", "E9E1C9", "C8A84B", "Torre de vigilancia montañesa", "Lanceros de montaña", "Guardia regia", "Defensa en bosque y escarpado", "Caballería más costosa", false),
            new KingdomDesign("leon", "León", "714186", "EAE1CE", "C5A351", "Cantería regia", "Infantería de escudo", "Caballería regia", "Murallas más asequibles", "Tiradores más costosos", false),
            new KingdomDesign("castilla", "Castilla", "922E2C", "D5AC4A", "EEE3C9", "Patio de mesnadas", "Ballesteros", "Caballería noble", "Lanceros más asequibles", "Caballería más costosa", false),
            new KingdomDesign("aragon", "Aragón", "D3A735", "A93431", "D6C6A3", "Lonja mercantil", "Infantería de frontera", "Caballería feudal", "Mercados más productivos", "Lanceros más costosos", false),
            new KingdomDesign("navarra", "Navarra", "8A263B", "C6A552", "555B5C", "Ferrería de montaña", "Tiradores de montaña", "Infanzones", "Defensa en terreno escarpado", "Caballería más costosa", false),
            new KingdomDesign("portugal", "Portugal", "245A84", "EEECE2", "47745C", "Casa de navegantes", "Infantería de frontera", "Caballería noble", "Mercados más productivos", "Infantería más costosa", false),
            new KingdomDesign("cordoba", "Córdoba", "E6D6B2", "356653", "BD963E", "Taller de artesanos", "Arqueros", "Guardia a caballo", "Primer mercado más asequible", "Lanceros más costosos", false),
            new KingdomDesign("sevilla", "Sevilla", "A65037", "E7D3A7", "407C79", "Alhóndiga", "Arqueros", "Guardia de taifa", "Almacenes más productivos", "Infantería más costosa", false),
            new KingdomDesign("zaragoza", "Zaragoza", "687448", "D5BF92", "477B8C", "Huerta de regadío", "Arqueros", "Caballería de frontera", "Pozos más productivos", "Caballería más costosa", false),
            new KingdomDesign("almoravides", "Almorávides", "B27A38", "314867", "E0C69A", "Acantonamiento de frontera", "Jinetes ligeros", "Guardia de élite", "Caballería más asequible", "Tiradores más costosos", false),
            new KingdomDesign("almohades", "Almohades", "E9E2D1", "8D3431", "B9984A", "Plaza de armas", "Infantería disciplinada", "Caballería de élite", "Lanceros más disciplinados", "Mercados más costosos", false),
            new KingdomDesign("granada", "Granada", "31785D", "963C3D", "E9DEC5", "Aljibe nazarí", "Arqueros", "Jinetes de élite", "Pozos más productivos", "Infantería más costosa", false),
            new KingdomDesign("visigodos", "Visigodos", "733944", "D4C19B", "A98547", "Aula regia", "Infantería de escudo", "Guardia regia", "Primera vivienda más asequible", "Caballería más costosa", true),
            new KingdomDesign("suevos", "Suevos", "4D654D", "D9CFB7", "A66A45", "Fortaleza de Gallaecia", "Infantería de colina", "Guardia del rey", "Defensa en bosque", "Tiradores más costosos", true),
            new KingdomDesign("bizancio", "Bizancio en Spania", "5D477B", "E9DFC7", "C69F48", "Puerto fortificado", "Arqueros de guarnición", "Caballería imperial", "Mercados más productivos", "Infantería más costosa", true),
            new KingdomDesign("barcelona", "Condado de Barcelona", "D4AA42", "A34235", "4B6470", "Mercado condal", "Ballesteros", "Caballería condal", "Mercados más productivos", "Lanceros más costosos", true)
        });

        public static KingdomDesign Find(string id) => All.FirstOrDefault(k => string.Equals(k.Id, id, StringComparison.Ordinal));
    }
}
