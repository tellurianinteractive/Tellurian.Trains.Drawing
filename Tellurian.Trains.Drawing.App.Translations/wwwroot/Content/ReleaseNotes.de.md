# Versionshinweise

## Version 0.6.0
Veröffentlicht am 2026-11-16
- Neues Modul: Ende B kann ein anderes Endprofil als Ende A haben, auch von einer anderen Norm im selben Maßstab, zum Beispiel FREMO an einem Ende und MMM am anderen. Markiere das Modul als Endewender, wenn ein asymmetrisches Endprofil die Seite wechselt. Ein asymmetrisches Endprofil wie FREMO-H0-B96 wird jetzt an Ende B gespiegelt, sodass die hohe Seite auf derselben Seite des Gleises bleibt und zwei solche Module ohne Abweichungen passen.
- Modulzeichnungen zeigen die Art des Endprofils wie DWG- und SKP-Zeichnungen: Die Form des Endprofils wird an jedem Ende ins Modul geklappt, dunkel, wo das Profil über Schienenoberkante liegt, und hell, wo es darunter liegt. Der Name des Endprofils steht entlang jedes Endes im Modul. Bezeichnungen und Pfeile der Enden werden nicht mehr gezeichnet.
- Endprofile haben einen Kurznamen, zum Beispiel *B96*, der in Modulzeichnungen angezeigt wird. Ohne Kurznamen wird die Bezeichnung angezeigt. Die eingebauten FREMO-Endprofile haben die Kurznamen *B96*, *B96f* und *2E96*.
- Modulzeichnungen füllen das Fenster, mit den Dateiaktionen daneben, und lassen sich drucken: *Drucken* druckt die Zeichnung allein mit dem Namen des Moduls, eingepasst in eine Seite im Querformat.
- Ein Modul wird durch das FREMO-Kürzel des Besitzers und eine FREMO-Nummer bezeichnet, zum Beispiel *SFjä 012*, durch einen Namen oder durch beides. Eine FREMO-Nummer braucht das Kürzel; ohne Nummer braucht das Modul einen Namen. Du kannst auch den vollständigen Namen des Besitzers angeben; das kann jemand anderes sein als du. Du gibst sie unter *Neues Modul* ein und änderst sie im Bereich *Eigenschaften* neben der Modulzeichnung. Die App zeigt die FREMO-Bezeichnung zusammen mit dem Namen.
- Die FREMO-Bezeichnung des Moduls, zum Beispiel *SFjä 7900*, oder sonst sein Name steht auf dem Modul, mit dem Namen des Besitzers in kleinerer Schrift darunter. Der Text verläuft entlang des Gleises, mittig zwischen den Enden, oberhalb des Gleises, so wie du ihn liest.

## Version 0.5.0
Veröffentlicht am 2026-11-15
- Zeichnungen werden als normale JSON-Dateien gespeichert, benannt nach ihrer Art, zum Beispiel *Station Nord.module.json* oder *FREMO-H0-B96.endprofile.json*. Sie lassen sich in jedem JSON-Editor öffnen. Dateien mit den früheren Endungen (.tdmodule, .tdendprofile usw.) lassen sich weiterhin öffnen.
- Öffnen und Importieren nehmen jede JSON-Datei an; die App liest die Art aus dem Inhalt. Andere JSON-Dateien in einem Ordner oder ZIP-Archiv werden übersprungen.
- Einstellung für den Maßstab: Wähle den Maßstab, in dem du arbeitest, dann werden nur Normen und Endprofile in diesem Maßstab angeboten, zusammen mit deinem Land.

## Version 0.4.0
Veröffentlicht am 2026-11-15
- Seite Einstellungen, unter Einstellungen im oberen Menü: dein Benutzername, das Land, in dem du arbeitest, und dein API-Schlüssel für das Modulregister. Die Einstellungen bleiben in deinem Browser, auch nach Updates, und werden nie in Dateien gespeichert.
- Änderungsprotokoll: Wenn du eine Zeichnung speicherst, werden dein Benutzername und das Datum darin festgehalten, ein Eintrag pro Person mit dem letzten Datum. Das Protokoll wird im Eigenschaftenbereich angezeigt. Ohne Benutzernamen wird nichts festgehalten, und die Problemanzeige zeigt eine Warnung.
- Normen, die nur in einigen Ländern gelten, wie MMM in Schweden, werden nur angeboten, wenn du eines dieser Länder in den Einstellungen wählst; ebenso ihre Zusätze und Endprofile. Zeichnungen, die sie verwenden, öffnen sich wie gewohnt. Deine eigenen Normen können ihre Länder angeben.
- Die Startseite zeigt die Versionsnummer nicht mehr; sie steht im oberen Menü.

## Version 0.3.0
Veröffentlicht am 2026-11-15
- Die Informationsseite ist jetzt die erste Seite, mit den Abschnitten Allgemein und Unterstützte Browser. Die Startseite mit Entwürfen und zuletzt verwendeten Dateien findest du unter Start im oberen Menü.
- Versionshinweise in der App: Klicke auf das Megafon neben der Versionsnummer. Sie werden in deiner Sprache angezeigt.
- Problemanzeige im oberen Menü: ein grüner Haken oder ein rotes Zeichen mit der Anzahl der Probleme in deinen eigenen Katalogeinträgen (Normen, Zusätze und Passungsausnahmen). Klicke auf ein Problem, um den Eintrag zu öffnen und zu korrigieren.
- Wähle die Sprache mit Flaggen im oberen Menü.
- Die App hat in jeder Sprache einen Namen: Modulzeichner, Module Draw, Modulritaren und Modultegneren.
- Die deutschen Texte sprechen dich mit „du“ an.
- Die Symbole funktionieren auch offline.

## Version 0.2.0
Veröffentlicht am 2026-11-15
- Erste öffentliche Vorabversion zum Ausprobieren. Die App heißt jetzt **Modulzeichner** (englisch Module Draw).
- Eine Informationsseite beim Start der App, die auch über „Modulzeichner“ im oberen Menü geöffnet wird.
- Editor für Endprofile: Zeichne die Oberkante des Kopfstücks nach einer bemaßten Zeichnung mit Linien, Bögen und Rundungen (mit der Seite, auf der der Mittelpunkt der Rundung liegt), setze Gleise und Schraubenlöcher, mit automatischen Maßen ab SOK oder ab der unteren linken Ecke des Kopfstücks.
- Ein Endprofil auf einer A4-Seite im Querformat drucken, mit Maßen in Papiergröße und dem Titel „Norm Maßstab Bezeichnung“.
- Module aus Vorlagen erstellen, mit einer Wartemeldung, während das Modul gezeichnet wird, und Endprofile, Module und Hallen ansehen.
- Katalog der Normen und Endprofile (FREMO, MMM), mit eigenen Normen, Zusätzen und Passungsausnahmen; Endprofile aus Dateien importieren.
- Weiterarbeiten: die Schaltflächen Speichern, Bearbeiten und Entwurf verwerfen für jeden Entwurf. Entwürfe werden automatisch im Browser gespeichert; speichere deine Zeichnungen in Dateien, um sie zu behalten und weiterzugeben.
- Funktioniert offline und kann als App installiert werden; Texte auf Englisch, Schwedisch, Deutsch, Dänisch und Norwegisch.
- Bekannte Einschränkungen: Die Editoren für Module, Hallen und Hallenpläne sind noch nicht fertig. Das Dateiformat kann sich vor Version 1.0 noch ändern. Speichern und Öffnen von Dateien funktioniert am besten in Chrome oder Edge auf einem Computer.

## Version 0.1.0
Veröffentlicht am 2026-11-13
- Erste Bereitstellung des App-Grundgerüsts: installierbar und offline nutzbar.
