# Versionsinformation

## Version 0.7.1
Utgiven 2026-11-17
- Appen ber webbläsaren att behålla dina inställningar och utkast så fort den startar. *Inställningar* visar om webbläsaren behåller dem, och kan be den igen.

## Version 0.7.0
Utgiven 2026-11-17
- Hallredigerare: *Ny hall…* på startsidan börjar med en rektangel. Rita andra väggar, dra hörn och väggar, eller skriv hörnen, väggarnas längder och bågar i panelen *Väggar*. Hallens mått anges i meter; du kan också skriva t.ex. 450 mm.
- Lägg till runda och fyrkantiga pelare, inredning, dörrar, nödutgångar och spärrade ytor. Markera något för att ändra storleken med dess handtag, och dra inuti det för att flytta det. Ett fästrutnät, 0,1 m om du inte väljer ett annat, gör det lätt att rita rektanglar.
- Dörrar stannar i sin vägg. En markerad dörr visar avstånden från hörnen till öppningen, som du också kan skriva.
- Markera dörrarna där moduler kan bäras in och ut; de får en blå dubbelpil. Nödutgångar är gröna, med en nödutgångsskylt som pekar mot utgången. Etiketter syns alltid överst.
- Hallen visar sin golvyta och sin användbara golvyta: utan pelare, inredning, ytorna som ska hållas fria framför dörrar och spärrade ytor.
- *Skriv ut* skriver ut hallen på en liggande sida, med storleken och golvytorna i mitten.
- Tal visas och skrivs som ditt språk skriver dem, t.ex. 12,5 på svenska och 12.5 på engelska.
- En knapp i övre högra hörnet av varje ritning visar hela ritningen igen, även för modulritningar.

## Version 0.6.0
Utgiven 2026-11-16
- Ny modul: ände B kan ha en annan ändprofil än ände A, även av en annan standard i samma skala, till exempel FREMO i ena änden och MMM i den andra. Markera modulen som ändevändare när en osymmetrisk ändprofil byter sida. En osymmetrisk ändprofil, som FREMO-H0-B96, speglas nu i ände B, så att den höga sidan stannar på samma sida om spåret och två sådana moduler passar utan avvikelser.
- Modulritningar visar typen av ändprofil som DWG- och SKP-ritningar gör: ändprofilens form viks in i modulen vid varje ände, mörk där profilen ligger över rälsöverkant och ljus där den ligger under. Ändprofilens namn står längs varje ände, inne i modulen. Ändarnas beteckningar och pilar ritas inte längre.
- Ändprofiler har ett kortnamn, till exempel *B96*, som visas i modulritningar. Utan kortnamn visas beteckningen. De inbyggda FREMO-ändprofilerna har kortnamnen *B96*, *B96f* och *2E96*.
- Modulritningar fyller fönstret, med filåtgärderna bredvid, och kan skrivas ut: *Skriv ut* skriver ut ritningen ensam med modulens namn, anpassad till en liggande sida.
- En modul identifieras med ägarens FREMO-prefix och ett FREMO-nummer, till exempel *SFjä 012*, med ett namn eller med båda. Ett FREMO-nummer kräver prefixet; utan nummer behöver modulen ett namn. Ägarens fullständiga namn kan också anges; ägaren kan vara någon annan än du. Ange dem i *Ny modul* och ändra dem i rutan *Egenskaper* bredvid modulritningen. Appen visar FREMO-beteckningen tillsammans med namnet.
- Modulens FREMO-beteckning, till exempel *SFjä 7900*, eller annars dess namn, står på modulen, med ägarens namn i mindre text under. Texten går längs spåret, centrerad mellan ändarna, ovanför spåret som du läser den.

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
