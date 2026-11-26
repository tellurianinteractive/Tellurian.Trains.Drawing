# Versionsinformation

## Version 0.11.0
Utgiven 2026-11-26
- *Hämta från Modulregistret* på sidan *Katalog* hämtar in registrets standarder och ändprofiler för din skala och ditt land, att välja bland som när du importerar en katalogfil.
- Ändprofiler som registret saknar ritning av märks *ingen ritning*. *Skapa ritning* ritar en från mallen och behåller dess beteckning och referens i registret; till dess kan den inte användas på en modul.
- En ändprofil som du har ritat själv, med samma beteckning, standard och skala som en som registret saknar ritning av, blir registrets när du hämtar: den behåller din ritning, och moduler som använder den flyttas till den när du öppnar dem.
- *Ladda upp till Modulregistret* i panelen *Arkiv* sparar en ändprofilsritning i registret som en ny revision. Det kräver din API-nyckel i *Inställningar*, och registret måste tillåta dig att hantera ändprofiler.

## Version 0.10.0
Utgiven 2026-11-24
- Appen har inte längre någon egen katalog: katalogen är din. Importera en katalogfil (.catalogue.json) på sidan *Katalog*, t.ex. från din klubb; den tar med sig sina skalor, standarder, tillägg och kompatibilitetsundantag tillsammans med de ändprofiler du väljer. Alla poster kan ändras och tas bort.
- Ändprofiler i en modul som din katalog saknar listas i panelen *Arkiv*, där du kan spara dem i katalogen.
- Modulredigerare, panelen *Sektioner*: *Antal sektioner* delar modulen i sektioner som är lika långa längs spåret, i en kurva också med samma vinkel. *Gör lika långa* jämnar ut de sektioner modulen har. En skarv i en kurva kan också placeras med sin *Vinkel från kurvans början*.
- Bara en driftplats har spår i panelen *Spår*; en modul med linje får inga om du inte lägger till dem. Varje spårs beteckning skrivs i ritningen.
- Under en rubrik med prefix, som redan säger vem som äger modulen, utelämnas ägarnas namn.
- Kortkommandon fungerar var du än är på sidan i alla redigerare, utom när du skriver i ett fält: 1–9 väljer verktyg, 0 anpassar ritningen till vyn, Ctrl+Z och Ctrl+Y ångrar och gör om, Delete tar bort, piltangenterna flyttar markeringen 1 mm (10 mm med Shift) och Escape avbryter. Hjälpen för varje redigerare listar dem.
- En ny version av filformatet, gemensam med de andra Tellurian Trains-apparna. Filer från tidigare versioner läses som förut och sparas i den nya versionen.
- En ritning som inte kan läsas berättar varför, i stället för att stoppa redigeraren.
- Efter *Vänd kurvan* anpassas vyn till hela modulen igen.

## Version 0.9.0
Utgiven 2026-11-18
- Modulredigerare: en ny panel *Ändar*. Ge ände A eller ände B en annan ändprofil i modulens skala, så ritas modulen om med den. *Vänd* vänder en osymmetrisk ändprofils platta andra sidan ut; *Ändevändare* följer med av sig själv.
- *Vänd kurvan* i panelen *Redigera* får en kurvad modul att svänga åt andra hållet. Sektionsskarvarna behåller sitt läge längs spåret; har ändplattan spåren vid sidan av mitten får du frågan först, eftersom deras brytpunkter flyttas.
- När du öppnar en modul uppdateras ändprofiler som har ändrats i din katalog. Panelen *Arkiv* visar vilka; spara modulen för att behålla dem i dess fil.
- Rubriken på en ändprofilritning utelämnar ord som upprepas, t.ex. *FREMO/H0-RE B96L* i stället för *FREMO/H0-RE H0 FREMO H0 B96L*.
- Hjälp: ett *?* på startsidan, i katalogen, i inställningarna och i varje panel i redigerarna öppnar hjälp om den, och många fält har en ledtråd under sig.
- Texterna säger *ändprofil* överallt, inte *platta*; hjälpen förklarar hur ändprofilen hänger ihop med modulgaveln.
- Informationssidan berättar vad den här förhandsversionen kan göra nu, och vad som inte är klart ännu.

## Version 0.8.0
Utgiven 2026-11-17
- Modulredigerare: dela en modul i sektioner. Välj *Sektionsskarv* och klicka där modulen delas; en rak skarv läggs till tvärs över spåret där. Sektionerna behöver inte vara lika långa, och deras längd längs spåret visas.
- Markera en skarv för att skriva dess exakta avstånd från början längs spåret, eller det exakta läget för var och en av dess punkter. Lägg till punkter för att få skarven att bryta, och dra skarvar och punkter i ritningen; ändarna stannar på modulens kant.
- Skarvar mellan sektioner passar alltid; ändprofiler jämförs bara där moduler möts i en hallplan.
- Modulens identifiering ändras bredvid ritningen och kan ångras.

## Version 0.7.2
Utgiven 2026-11-17
- *Inställningar* berättar hur du får den webbläsare du använder att behålla dina inställningar och utkast, t.ex. som ett undantag i Firefox sekretessinställningar, med den här webbplatsens adress att kopiera.
- Exportera dina inställningar och ditt språk till en fil, och importera dem igen om webbläsaren har glömt dem, eller i en annan webbläsare.
- Informationssidan berättar var dina inställningar och utkast sparas.

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
- Redigerare för ändprofiler: rita ändprofilens överkant efter en måttsatt ritning med linjer, bågar och avrundningar (med vilken sida avrundningens centrum ligger på), placera spår och skruvhål, med automatiska mått från RÖK eller från ändprofilens nedre vänstra hörn.
- Skriv ut en ändprofil på en liggande A4-sida, med mått i pappersstorlek och titeln "Standard Skala Beteckning".
- Skapa moduler från mallar, med ett vänta-meddelande medan modulen ritas, och visa ändprofiler, moduler och hallar.
- Katalog med standarder och ändprofiler (FREMO, MMM), med egna standarder, tillägg och undantag för passning; importera ändprofiler från filer.
- Fortsätt arbeta: knapparna Spara, Redigera och Släng utkastet för varje utkast. Utkast sparas automatiskt i webbläsaren; spara dina ritningar till filer för att behålla och dela dem.
- Fungerar utan uppkoppling och kan installeras som en app; texter på engelska, svenska, tyska, danska och norska.
- Kända begränsningar: redigerarna för moduler, hallar och hallplaner är inte klara än. Filformatet kan ändras före version 1.0. Att spara och öppna filer fungerar bäst i Chrome eller Edge på en dator.

## Version 0.1.0
Utgiven 2026-11-13
- Första driftsättningen av appens grundstomme: kan installeras och fungerar utan uppkoppling.
