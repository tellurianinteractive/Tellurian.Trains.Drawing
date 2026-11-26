# Versionshinweise

## Version 0.11.0
Veröffentlicht am 2026-11-26
- *Aus dem Modulregister holen* auf der Seite *Katalog* holt die Normen und Endprofile des Registers für deinen Maßstab und dein Land herein, zur Auswahl wie beim Import einer Katalogdatei.
- Endprofile, von denen das Register keine Zeichnung hat, sind mit *keine Zeichnung* markiert. *Zeichnung erstellen* zeichnet eine aus der Vorlage und behält Bezeichnung und Verweis ins Register; bis dahin kann es nicht an einem Modul verwendet werden.
- Ein Endprofil, das du selbst gezeichnet hast, mit derselben Bezeichnung, Norm und demselben Maßstab wie eines, von dem das Register keine Zeichnung hat, wird beim Holen das des Registers: Es behält deine Zeichnung, und Module, die es verwenden, werden beim Öffnen darauf umgestellt.
- *Ins Modulregister hochladen* im Bereich *Archiv* speichert eine Endprofilzeichnung als neue Revision im Register. Dafür braucht es deinen API-Schlüssel in den *Einstellungen*, und das Register muss dir erlauben, Endprofile zu verwalten.

## Version 0.10.0
Veröffentlicht am 2026-11-24
- Die App hat keinen eigenen Katalog mehr: Der Katalog gehört dir. Importiere auf der Seite *Katalog* eine Katalogdatei (.catalogue.json), z. B. von deinem Verein; sie bringt ihre Maßstäbe, Normen, Zusätze und Kompatibilitätsausnahmen mit den Endprofilen mit, die du wählst. Jeder Eintrag kann geändert und gelöscht werden.
- Endprofile in einem Modul, die deinem Katalog fehlen, werden im Bereich *Archiv* aufgelistet, wo du sie im Katalog speichern kannst.
- Moduleditor, Bereich *Segmente*: *Anzahl Segmente* teilt das Modul in Segmente gleicher Länge entlang des Gleises, in einer Kurve auch mit gleichem Winkel. *Gleich lang machen* gleicht die Segmente an, die das Modul hat. Ein Stoß in einer Kurve kann auch über seinen *Winkel vom Anfang der Kurve* platziert werden.
- Nur eine Betriebsstelle hat Gleise im Bereich *Gleise*; ein Streckenmodul bekommt keine, außer du fügst sie hinzu. Die Bezeichnung jedes Gleises steht in der Zeichnung.
- Unter einem Titel mit Präfix, der schon sagt, wem das Modul gehört, werden die Namen der Besitzer weggelassen.
- Tastenkürzel funktionieren in jedem Editor überall auf der Seite, außer während du in ein Feld schreibst: 1–9 wählen ein Werkzeug, 0 passt die Zeichnung in die Ansicht ein, Strg+Z und Strg+Y machen rückgängig und wiederholen, Entf löscht, die Pfeiltasten verschieben die Auswahl um 1 mm (10 mm mit Umschalt) und Esc bricht ab. Die Hilfe jedes Editors listet sie auf.
- Eine neue Version des Dateiformats, gemeinsam mit den anderen Tellurian-Trains-Apps. Dateien früherer Versionen werden wie bisher gelesen und in der neuen Version gespeichert.
- Eine Zeichnung, die nicht gelesen werden kann, sagt warum, statt den Editor anzuhalten.
- Nach *Kurve spiegeln* wird die Ansicht wieder an das ganze Modul angepasst.

## Version 0.9.0
Veröffentlicht am 2026-11-18
- Moduleditor: ein neuer Bereich *Enden*. Gib Ende A oder Ende B ein anderes Endprofil im Maßstab des Moduls, und das Modul wird damit neu gezeichnet. *Wenden* dreht das Kopfstück eines asymmetrischen Endprofils mit der anderen Seite nach außen; *Endewender* folgt von selbst.
- *Kurve spiegeln* im Bereich *Bearbeiten* lässt ein gebogenes Modul in die andere Richtung drehen. Die Segmentstöße behalten ihre Lage entlang des Gleises; liegen die Gleise außerhalb der Mitte des Kopfstücks, wirst du vorher gefragt, da sich ihre Knicke verschieben.
- Wenn du ein Modul öffnest, werden Endprofile aktualisiert, die sich in deinem Katalog geändert haben. Der Bereich *Archiv* zeigt, welche; speichere das Modul, damit sie in seiner Datei bleiben.
- Der Titel einer Endprofilzeichnung lässt Wörter weg, die sich wiederholen, z. B. *FREMO/H0-RE B96L* statt *FREMO/H0-RE H0 FREMO H0 B96L*.
- Hilfe: Ein *?* auf der Startseite, im Katalog, in den Einstellungen und in jedem Bereich der Editoren öffnet die Hilfe dazu, und viele Felder haben einen Hinweis darunter.
- Die Texte sagen überall *Endprofil* statt *Kopfstück*; die Hilfe erklärt, wie das Endprofil mit dem Brett am Modulende zusammenhängt.
- Die Informationsseite sagt, was diese Vorabversion jetzt kann und was noch nicht fertig ist.

## Version 0.8.0
Veröffentlicht am 2026-11-17
- Moduleditor: Teile ein Modul in Segmente. Wähle *Segmentstoß* und klicke dort, wo das Modul geteilt ist; dort wird ein gerader Stoß quer zum Gleis hinzugefügt. Die Segmente müssen nicht gleich lang sein, und ihre Länge entlang des Gleises wird angezeigt.
- Wähle einen Stoß aus, um seinen genauen Abstand vom Anfang entlang des Gleises oder die genaue Lage jedes seiner Punkte einzugeben. Füge Punkte hinzu, damit der Stoß abknickt, und ziehe Stöße und Punkte in der Zeichnung; die Enden bleiben am Modulrand.
- Stöße zwischen Segmenten passen immer; Endprofile werden nur dort verglichen, wo Module in einem Hallenplan aufeinandertreffen.
- Die Kennzeichnung des Moduls wird neben der Zeichnung geändert und kann rückgängig gemacht werden.

## Version 0.7.2
Veröffentlicht am 2026-11-17
- *Einstellungen* erklärt, wie dein Browser deine Einstellungen und Entwürfe behält, z. B. als Ausnahme in den Datenschutzeinstellungen von Firefox, mit der Adresse dieser Website zum Kopieren.
- Exportiere deine Einstellungen und deine Sprache in eine Datei und importiere sie wieder, wenn der Browser sie vergessen hat, oder in einem anderen Browser.
- Die Informationsseite erklärt, wo deine Einstellungen und Entwürfe gespeichert werden.

## Version 0.7.1
Veröffentlicht am 2026-11-17
- Die App bittet den Browser gleich beim Start, deine Einstellungen und Entwürfe zu behalten. *Einstellungen* zeigt, ob der Browser sie behält, und kann ihn erneut bitten.

## Version 0.7.0
Veröffentlicht am 2026-11-17
- Halleneditor: *Neue Halle…* auf der Startseite beginnt mit einem Rechteck. Zeichne andere Wände, ziehe Ecken und Wände oder gib die Ecken, Wandlängen und Bögen im Bereich *Wände* ein. Hallenmaße sind in Metern; du kannst auch z. B. 450 mm eingeben.
- Füge runde und eckige Säulen, Einbauten, Türen, Notausgänge und Sperrflächen hinzu. Wähle etwas aus, um seine Größe mit den Griffen zu ändern, und ziehe darin, um es zu verschieben. Ein Fangraster, 0,1 m, wenn du kein anderes wählst, macht Rechtecke leicht.
- Türen bleiben in ihrer Wand. Eine ausgewählte Tür zeigt ihre Abstände von den Ecken bis zur Öffnung, die du auch eingeben kannst.
- Markiere die Türen, durch die Module hinein- und hinausgetragen werden können; sie bekommen einen blauen Doppelpfeil. Notausgänge sind grün, mit einem Notausgangszeichen, das zum Ausgang zeigt. Beschriftungen sind immer oben lesbar.
- Die Halle zeigt ihre Bodenfläche und ihre nutzbare Bodenfläche: ohne Säulen, Einbauten, die Flächen, die vor Türen frei bleiben müssen, und Sperrflächen.
- *Drucken* druckt die Halle auf einer Seite im Querformat, mit ihrer Größe und den Bodenflächen in der Mitte.
- Zahlen werden so angezeigt und eingegeben, wie deine Sprache sie schreibt, z. B. 12,5 auf Deutsch und 12.5 auf Englisch.
- Eine Schaltfläche in der oberen rechten Ecke jeder Zeichnung zeigt wieder die ganze Zeichnung, auch bei Modulzeichnungen.

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
- Editor für Endprofile: Zeichne die Oberkante des Endprofils nach einer bemaßten Zeichnung mit Linien, Bögen und Rundungen (mit der Seite, auf der der Mittelpunkt der Rundung liegt), setze Gleise und Schraubenlöcher, mit automatischen Maßen ab SOK oder ab der unteren linken Ecke des Endprofils.
- Ein Endprofil auf einer A4-Seite im Querformat drucken, mit Maßen in Papiergröße und dem Titel „Norm Maßstab Bezeichnung“.
- Module aus Vorlagen erstellen, mit einer Wartemeldung, während das Modul gezeichnet wird, und Endprofile, Module und Hallen ansehen.
- Katalog der Normen und Endprofile (FREMO, MMM), mit eigenen Normen, Zusätzen und Passungsausnahmen; Endprofile aus Dateien importieren.
- Weiterarbeiten: die Schaltflächen Speichern, Bearbeiten und Entwurf verwerfen für jeden Entwurf. Entwürfe werden automatisch im Browser gespeichert; speichere deine Zeichnungen in Dateien, um sie zu behalten und weiterzugeben.
- Funktioniert offline und kann als App installiert werden; Texte auf Englisch, Schwedisch, Deutsch, Dänisch und Norwegisch.
- Bekannte Einschränkungen: Die Editoren für Module, Hallen und Hallenpläne sind noch nicht fertig. Das Dateiformat kann sich vor Version 1.0 noch ändern. Speichern und Öffnen von Dateien funktioniert am besten in Chrome oder Edge auf einem Computer.

## Version 0.1.0
Veröffentlicht am 2026-11-13
- Erste Bereitstellung des App-Grundgerüsts: installierbar und offline nutzbar.
