namespace OmniGenesis
{
    // Auszüge aus dem Kaiserlichen Edikt von Omni Genesis (Vorlage des Autors), sinngemäß ins Deutsche übertragen.
    // Jeder Auszug nennt, welche Spielmechanik ihn umsetzt.
    public static class EdictTexts
    {
        public struct Article
        {
            public string number;
            public string title;
            public string text;
            public string mechanic;
        }

        public const string Preamble =
            "Ein Kontinent. Ein Thron. Ein Gesetz.\n\n" +
            "Mortem Underwood, Oberster Kaiser des Großen Reiches Omni Genesis, Träger des Throns der Genesis, " +
            "verkündet dieses Edikt allen Völkern, Großen Häusern, Gilden und Landen. Aus der Asche der Ära der Spaltung " +
            "erhob sich der Thron der Genesis und vereinte den Kontinent. Würdigen lässt das Gesetz zwei Wege zu einer " +
            "eigenen Macht: den Weg der Diplomatie und den Weg des Krieges. Einen dritten Weg gibt es nicht.";

        public static readonly Article[] Articles =
        {
            new Article { number = "Art. 11", title = "Völker des Kontinents", text = "Das Reich fördert die Einheit von Menschen, Zwergen, Elfen und anderen Völkern und schützt ihre Künste.", mechanic = "Wahl der Rasse bei der Charaktererstellung." },
            new Article { number = "Art. 16", title = "Siegel und Gleichgewicht", text = "Das Reich bewahrt das Gleichgewicht der Natur und des Äthers und hütet die Siegel, die Risse und alte Mächte eindämmen.", mechanic = "Siegel der Eindämmung am Riss; schwache Siegel lassen Leerenbrut frei." },
            new Article { number = "Art. 20", title = "Symbole", text = "Das Reichsbanner ist mitternachtsazurblau mit dem goldenen achtzackigen Stern der Genesis. Hauptstadt ist Grad Genesis.", mechanic = "Banner in Grad Genesis, Krone der Acht Sterne des Kaisers." },
            new Article { number = "Art. 24", title = "Gleichheit", text = "Bürger sind vor dem Gesetz gleich, unabhängig von Rasse, Herkunft, Rang oder magischer Gabe.", mechanic = "Alle Rassen und Klassen haben dasselbe Wertebudget." },
            new Article { number = "Art. 27", title = "Recht auf Leben", text = "Niemand darf willkürlich seines Lebens beraubt werden.", mechanic = "Angriffe auf Bürger sind Verbrechen." },
            new Article { number = "Art. 29", title = "Festnahme", text = "Festnahme nur durch Gericht oder auf frischer Tat. Der Festgenommene wird binnen drei Tagen vor Gericht gestellt.", mechanic = "Gesuchte werden festgenommen; das Urteil folgt binnen drei Spieltagen." },
            new Article { number = "Art. 33", title = "Recht des Freien Weges", text = "Jeder Bürger darf frei reisen. Niemand darf seine Lande für Bürger schließen.", mechanic = "Keine gesperrten Wege — außer den Verbotenen Landen. Pflichtgesetz jeder Autonomie." },
            new Article { number = "Art. 34", title = "Große Tore", text = "Trakte, Himmelswege und Große Tore stehen jedem offen. Die Torgebühr setzt der Thron einheitlich fest.", mechanic = "Schnellreise durch die Großen Tore für 25 Taler." },
            new Article { number = "Art. 45", title = "Heilung", text = "Jeder hat das Recht auf Heilung in den kaiserlichen Lazaretten.", mechanic = "Kostenlose Heilung im Lazarett; dort erwacht man nach einer Niederlage." },
            new Article { number = "Art. 46", title = "Schutz vor Rissgeschöpfen", text = "Jeder hat das Recht auf Schutz vor den Geschöpfen der Risse und der Leere.", mechanic = "Wachen und Ordensmagier bekämpfen Leerenbrut." },
            new Article { number = "Art. 48", title = "Wege der Meisterschaft", text = "Jeder darf einen Weg der Meisterschaft wählen und in den Rängen des Aufstiegs steigen. Ränge vergibt die Kaiserliche Prüfung.", mechanic = "Klassen und Ränge F–SSS durch die Prüfung in der Arena; Kapitel I der Geschichte." },
            new Article { number = "Art. 87", title = "Kaiserliche Kanzlei", text = "Die Kanzlei nimmt Bittschriften und Petitionen an, führt die Register und stellt das Siegel des Bürgers aus.", mechanic = "Kapitel I: Anmeldung bei Richter Ataklas." },
            new Article { number = "Art. 54", title = "Verkündung", text = "Edikt und Ukase werden durch die Großen Tore verkündet.", mechanic = "Verkündungen erscheinen an allen Großen Toren." },
            new Article { number = "Art. 57", title = "Unschuldsvermutung", text = "Jeder gilt als unschuldig, bis seine Schuld vor Gericht bewiesen ist.", mechanic = "Nur bezeugte Taten führen zu einem Urteil." },
            new Article { number = "Art. 58", title = "Keine Doppelbestrafung", text = "Niemand wird für dieselbe Tat zweimal bestraft.", mechanic = "Nach dem Urteil ist die Tat gesühnt." },
            new Article { number = "Art. 60", title = "Pflichten", text = "Das Edikt achten, den Kontinent beim Großen Aufruf verteidigen, die Siegel hüten, Abgaben zahlen, keine Verbotenen Künste.", mechanic = "Siegel verstärken, Abgaben auf Handel, Großer Aufruf." },
            new Article { number = "Art. 62", title = "Unantastbarkeit des Kaisers", text = "Ein Anschlag auf Leben, Ehre und Macht des Kaisers ist Großer Verrat.", mechanic = "Ein Angriff auf den Kaiser verwirkt für immer das Recht, Prätendent zu werden." },
            new Article { number = "Art. 66", title = "Großer Aufruf", text = "Der Kaiser ruft den Kontinent angesichts einer Bedrohung zur Mobilmachung.", mechanic = "Fällt das Siegel unter 15 %, beginnt der Große Aufruf." },
            new Article { number = "Art. 69", title = "Höchste Ränge", text = "Der Kaiser verleiht Titel, Lehen und die höchsten Ränge des Aufstiegs.", mechanic = "Die Ränge S, SS und SSS verleiht der Kaiser." },
            new Article { number = "Art. 93", title = "Abgaben", text = "Abgaben setzt allein der Thron fest; sie sind auf dem ganzen Kontinent gleich.", mechanic = "Einheitliche Handelsabgabe von 10 % an die Kaiserliche Kasse." },
            new Article { number = "Art. 112", title = "Verbotene Lande", text = "Die Verbotenen Lande dürfen nur mit kaiserlicher Erlaubnis betreten werden. Der Orden der Kampfmagier bewacht sie.", mechanic = "Erlaubnis in der Kanzlei (ab Rang D). Ohne sie greifen die Ordensmagier an." },
            new Article { number = "Art. 121", title = "Gilden", text = "Eine Gilde handelt nach der Charta des Throns. Gilden der Magier, Meister, Händler, Ruinensucher und Söldner sind vor dem Gesetz gleich.", mechanic = "Gildenhaus der Abenteurer: tägliche Aufträge bei Runa." },
            new Article { number = "Art. 116–117", title = "Miete", text = "Häuser, Werkstätten und Land werden gemietet. Einnahmen erhält der Eigentümer der Region, mit Abgabe an die Kasse.", mechanic = "Zimmer im Mietshaus: Bett und Truhe." },
            new Article { number = "Art. 127–128", title = "Weg der Diplomatie", text = "Ein würdiger Anführer darf auf dem Land des Kontinents eine eigene Autonomie gründen — durch Vertrag mit dem Thron.", mechanic = "Endziel: Eichenhain als eigene Autonomie." },
            new Article { number = "Art. 129", title = "Der Prätendent", text = "Würdig an Seele (kein Verrat, keine Meuterei, keine Verbotenen Künste) und würdig an Kraft.", mechanic = "Stufe mindestens wie die des Kaisers (100) und ein reines Register." },
            new Article { number = "Art. 130–133", title = "Drei Säulen", text = "Säule der Ressourcen, Säule des Volkes, Säule der Souveränität. Fehlt eine, ist kein Vertrag möglich.", mechanic = "Fenster „Drei Säulen“ (Taste P)." },
            new Article { number = "Art. 134", title = "Petition und Revision", text = "Die Petition geht über die Kanzlei an Richter Ataklas. Die Arbiter prüfen jeden Beweis vor Ort.", mechanic = "Revision über mehrere Spieltage; fällt eine Säule, wird abgelehnt." },
            new Article { number = "Art. 135", title = "Audienz", text = "Richter Ataklas bestimmt Tag und Stunde der Audienz. Wer nicht erscheint, verliert die Petition. Der Kaiser prüft den Prätendenten mit seinem Blick.", mechanic = "Audienz zur festen Stunde im Thronsaal." },
            new Article { number = "Art. 136", title = "Vertrag", text = "Der Vertrag wird mit dem Großen Siegel der Genesis besiegelt und bestimmt Name, Wappen und Grenzen der Macht.", mechanic = "Name und Farbe der Autonomie, eigene Banner." },
            new Article { number = "Art. 140", title = "Auflösung", text = "Fällt eine der Drei Säulen, sendet der Thron eine Vorwarnung mit Frist; danach wird der Vertrag aufgelöst.", mechanic = "Tägliche Prüfung nach dem Vertrag." },
            new Article { number = "Art. 141, 153", title = "Meuterei", text = "Meuterei ist ein geheimer oder heimtückischer Aufstand gegen den Thron ohne Manifest. Sie ist ein Verbrechen, kein Weg, und wird mit aller Macht niedergeschlagen.", mechanic = "Hauptgeschichte: die Meuterei von Haus Vorn." },
            new Article { number = "Art. 141–154", title = "Weg des Krieges", text = "Offene Herausforderung durch Manifest, Vorbereitungszeit, Feld der Prüfung und Duell des Throns mit dem Kaiser selbst.", mechanic = "In Vorbereitung (nächster Entwicklungsschritt)." },
        };
    }
}
