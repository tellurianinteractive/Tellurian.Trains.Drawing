# Betriebsstelle

Eine Betriebsstelle ist das, was ein Fahrplaner vom Modul sieht: eine Stelle, an der Züge halten, sich begegnen oder geleitet werden. Ein Modul ohne Betriebsstelle ist freie Strecke.

- **Zur Betriebsstelle machen** macht das Modul zu einem Bahnhof mit dem Namen des Moduls. Gib ihm eine Signatur.
- **Betriebsstelle entfernen** macht das Modul wieder zur freien Strecke. Die Gleise bleiben im Bereich *Gleise*.

## Die Eigenschaften

- **Name** und **Signatur**: Die Signatur ist der Kurzname, der in Fahrplänen verwendet wird, z. B. *Tby*. Sie ist innerhalb des Landes eindeutig.
- **Land**: das Land, als dessen Teil die Betriebsstelle betrieben wird, als Code mit zwei Buchstaben, z. B. *SE*.
- **Art**:
  - **Bahnhof**: Züge halten, um sich zu begegnen, überholt zu werden oder Reisende oder Fracht zu wechseln.
  - **Signalgesteuerte Stelle**: eine Blockstelle, Abzweigstelle oder Kreuzungsstelle. Züge halten dort nie.
  - **Industriegebiet**: nur Fracht.
  - **Sonstige Stelle**: z. B. ein Haltepunkt. Nur Reisende.
- Die Kästchen sagen, was an der Stelle möglich ist. Was die Art nicht haben kann, ist ausgeschaltet, und was sie immer hat, ist eingeschaltet. Wenn du die Art änderst, wirst du gefragt, bevor etwas entfernt wird.
- **Züge können kreuzen** wird für eine signalgesteuerte Stelle gewählt. In einem Bahnhof können Züge kreuzen, wenn er mehr als ein Gleis im Fahrplan hat.
- **Schattenbahnhof**: ein Bahnhof, der für die Welt außerhalb der Anlage steht. Dort werden immer Reisende und Fracht gewechselt.
- **Mitte**: Entfernungen zu anderen Betriebsstellen werden von der Mitte aus gemessen, z. B. vom Empfangsgebäude, entlang des Hauptgleises. Sie liegt in der Mitte des Moduls, bis du **Mitte in der Mitte des Moduls** abwählst und die Entfernung vom linken Ende eingibst.

Probleme werden rot angezeigt, halten dich aber nicht auf. Änderungen lassen sich rückgängig machen.
