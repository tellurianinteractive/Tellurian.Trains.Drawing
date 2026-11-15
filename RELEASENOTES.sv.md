# Versionsinformation

## Version 0.5.0
Utgiven 2026-11-15
- Ritningar sparas som vanliga JSON-filer, namngivna efter sin typ, till exempel *Station Nord.module.json* eller *FREMO-H0-B96.endprofile.json*. De öppnas i vilken JSON-redigerare som helst. Filer som sparats med de tidigare filändelserna (.tdmodule, .tdendprofile och så vidare) går fortfarande att öppna.
- Öppna och importera tar emot alla JSON-filer; appen läser typen ur innehållet. Andra JSON-filer i en mapp eller ett ZIP-arkiv hoppas över.
- Inställning för skala: välj skalan du arbetar i, så visas bara standarder och ändprofiler i den skalan, tillsammans med ditt land.

## Version 0.4.0
Utgiven 2026-11-15
- Sidan Inställningar, under Inställningar i toppmenyn: ditt användarnamn, landet du arbetar i och din API-nyckel för Modulregistret. Inställningarna sparas i webbläsaren, även efter uppdateringar, och sparas aldrig i filer.
- Ändringslogg: när du sparar en ritning registreras ditt användarnamn och datumet i den, en rad per person med det senaste datumet. Loggen visas i egenskapspanelen. Utan användarnamn registreras ingenting, och problemindikatorn visar en varning.
- Standarder som bara gäller i vissa länder, som MMM i Sverige, visas bara när du väljer ett av dessa länder i inställningarna; likaså deras tillägg och ändprofiler. Ritningar som använder dem öppnas som vanligt. Dina egna standarder kan ange sina länder.
- Startsidan visar inte längre versionsnumret; det finns i toppmenyn.

## Version 0.3.0
Utgiven 2026-11-15
- Informationssidan är nu första sidan, med avsnitten Allmänt och Webbläsare som stöds. Startsidan med utkast och senaste filer finns under Start i toppmenyn.
- Versionsinformation i appen: klicka på megafonen bredvid versionsnumret. Den visas på ditt språk.
- Problemindikator i toppmenyn: en grön bock, eller en röd symbol med antalet problem i dina egna katalogposter (standarder, tillägg och undantag för passning). Klicka på ett problem för att öppna posten och rätta den.
- Välj språk med flaggor i toppmenyn.
- Appen har ett namn på varje språk: Modulritaren, Module Draw, Modulzeichner och Modultegneren.
- De tyska texterna använder "du".
- Ikonerna fungerar även utan uppkoppling.

## Version 0.2.0
Utgiven 2026-11-15
- Första publika förhandsversionen, att prova på. Appen heter nu **Modulritaren** (Module Draw på engelska).
- En informationssida när appen startar, som också öppnas från "Modulritaren" i toppmenyn.
- Redigerare för ändprofiler: rita ändplattans överkant efter en måttsatt ritning med linjer, bågar och avrundningar (med vilken sida avrundningens centrum ligger på), placera spår och skruvhål, med automatiska mått från RÖK eller från plattans nedre vänstra hörn.
- Skriv ut en ändprofil på en liggande A4-sida, med mått i pappersstorlek och titeln "Standard Skala Beteckning".
- Skapa moduler från mallar, med ett vänta-meddelande medan modulen ritas, och visa ändprofiler, moduler och hallar.
- Katalog med standarder och ändprofiler (FREMO, MMM), med egna standarder, tillägg och undantag för passning; importera ändprofiler från filer.
- Fortsätt arbeta: knapparna Spara, Redigera och Släng utkastet för varje utkast. Utkast sparas automatiskt i webbläsaren; spara dina ritningar till filer för att behålla och dela dem.
- Fungerar utan uppkoppling och kan installeras som en app; texter på engelska, svenska, tyska, danska och norska.
- Kända begränsningar: redigerarna för moduler, hallar och hallplaner är inte klara än. Filformatet kan ändras före version 1.0. Att spara och öppna filer fungerar bäst i Chrome eller Edge på en dator.

## Version 0.1.0
Utgiven 2026-11-13
- Första driftsättningen av appens grundstomme: kan installeras och fungerar utan uppkoppling.
