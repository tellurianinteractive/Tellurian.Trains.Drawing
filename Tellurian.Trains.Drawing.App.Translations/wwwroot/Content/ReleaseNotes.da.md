# Udgivelsesnoter

## Version 0.11.1
Udgivet 2026-11-26
- Knapper, der læser eller gemmer data, viser en snurre, mens de arbejder, og sidens øvrige af den slags knapper venter, til de er færdige.
- Standarder fra Modulregistret flettes med dine med samme navn, så kataloget aldrig har to standarder med samme navn. Registrets oplysninger går forud.
- En standard, der udvider en anden, fx fra Modulregistret, tilbyder endeprofilerne for den standard, den udvider.
- En standard i dit katalog har kun et kortnavn, en skala og lande; sporvidden angives for hvert spor i endeprofilerne. Efter hentning fra Modulregistret vises dets standarder ikke længere som problemer.
- En ny endeprofil foreslår sporvidden fra standardens eksisterende endeprofiler.

## Version 0.11.0
Udgivet 2026-11-26
- *Hent fra Modulregistret* på siden *Katalog* henter registrets standarder og endeprofiler for din skala og dit land ind, at vælge imellem som når du importerer en katalogfil.
- Endeprofiler, som registret ikke har en tegning af, er markeret *ingen tegning*. *Opret tegning* tegner en ud fra skabelonen og beholder dens betegnelse og reference i registret; indtil da kan den ikke bruges på et modul.
- En endeprofil, du selv har tegnet, med samme betegnelse, standard og skala som en, registret ikke har en tegning af, bliver registrets, når du henter: den beholder din tegning, og moduler, der bruger den, flyttes til den, når du åbner dem.
- *Upload til Modulregistret* i panelet *Arkiv* gemmer en endeprofiltegning i registret som en ny revision. Det kræver din API-nøgle i *Indstillinger*, og registret skal tillade dig at administrere endeprofiler.

## Version 0.10.0
Udgivet 2026-11-24
- Appen har ikke længere sit eget katalog: kataloget er dit. Importér en katalogfil (.catalogue.json) på siden *Katalog*, fx fra din klub; den har sine skalaer, standarder, tilføjelser og kompatibilitetsundtagelser med sammen med de endeprofiler, du vælger. Alle poster kan ændres og slettes.
- Endeprofiler i et modul, som dit katalog mangler, vises i panelet *Arkiv*, hvor du kan gemme dem i kataloget.
- Moduleditor, panelet *Sektioner*: *Antal sektioner* deler modulet i sektioner, der er lige lange langs sporet, i en kurve også med samme vinkel. *Gør lige lange* udligner de sektioner, modulet har. En samling i en kurve kan også placeres med sin *Vinkel fra kurvens start*.
- Kun et driftssted har spor i panelet *Spor*; et strækningsmodul får ingen, medmindre du tilføjer dem. Hvert spors betegnelse skrives i tegningen.
- Under en titel med præfiks, som allerede siger, hvem der ejer modulet, udelades ejernes navne.
- Tastaturgenveje virker overalt på siden i alle editorer, undtagen mens du skriver i et felt: 1–9 vælger et værktøj, 0 tilpasser tegningen til visningen, Ctrl+Z og Ctrl+Y fortryder og gentager, Delete sletter, piletasterne flytter markeringen 1 mm (10 mm med Shift), og Escape afbryder. Hjælpen til hver editor viser dem.
- En ny version af filformatet, fælles med de andre Tellurian Trains-apps. Filer fra tidligere versioner læses som før og gemmes i den nye version.
- En tegning, der ikke kan læses, fortæller hvorfor i stedet for at stoppe editoren.
- Efter *Vend kurven* tilpasses visningen igen til hele modulet.

## Version 0.9.0
Udgivet 2026-11-18
- Moduleditor: et nyt panel *Ender*. Giv ende A eller ende B en anden endeprofil i modulets skala, så tegnes modulet igen med den. *Vend* vender en asymmetrisk endeprofils plade med den anden side udad; *Endevender* følger med af sig selv.
- *Vend kurven* i panelet *Redigér* får et kurvet modul til at dreje den anden vej. Sektionssamlingerne beholder deres placering langs sporet; ligger sporene uden for endepladens midte, bliver du spurgt først, da deres knæk flyttes.
- Når du åbner et modul, opdateres endeprofiler, der er ændret i dit katalog. Panelet *Arkiv* viser hvilke; gem modulet for at beholde dem i dets fil.
- Titlen på en endeprofiltegning udelader ord, der gentages, f.eks. *FREMO/H0-RE B96L* i stedet for *FREMO/H0-RE H0 FREMO H0 B96L*.
- Hjælp: et *?* på startsiden, i kataloget, i indstillingerne og i hvert panel i editorerne åbner hjælp om det, og mange felter har et tip under sig.
- Teksterne siger *endeprofil* overalt, ikke *plade*; hjælpen forklarer, hvordan endeprofilen hænger sammen med endepladen.
- Informationssiden fortæller, hvad denne forhåndsversion kan nu, og hvad der ikke er klar endnu.

## Version 0.8.0
Udgivet 2026-11-17
- Moduleditor: del et modul i sektioner. Vælg *Sektionssamling* og klik, hvor modulet er delt; der tilføjes en lige samling på tværs af sporet. Sektionerne behøver ikke være lige lange, og deres længde langs sporet vises.
- Markér en samling for at skrive dens nøjagtige afstand fra starten langs sporet eller den nøjagtige placering af hvert af dens punkter. Tilføj punkter for at få samlingen til at knække, og træk samlinger og punkter i tegningen; enderne bliver på modulets kant.
- Samlinger mellem sektioner passer altid; endeprofiler sammenlignes kun, hvor moduler mødes i en hal-plan.
- Modulets identifikation ændres ved siden af tegningen og kan fortrydes.

## Version 0.7.2
Udgivet 2026-11-17
- *Indstillinger* fortæller, hvordan du får den browser, du bruger, til at beholde dine indstillinger og udkast, f.eks. som en undtagelse i Firefox' privatlivsindstillinger, med dette websteds adresse til at kopiere.
- Eksportér dine indstillinger og dit sprog til en fil, og importér dem igen, hvis browseren har glemt dem, eller i en anden browser.
- Informationssiden fortæller, hvor dine indstillinger og udkast gemmes.

## Version 0.7.1
Udgivet 2026-11-17
- Appen beder browseren om at beholde dine indstillinger og udkast, så snart den starter. *Indstillinger* viser, om browseren beholder dem, og kan bede den igen.

## Version 0.7.0
Udgivet 2026-11-17
- Haleditor: *Ny hal…* på startsiden starter med et rektangel. Tegn andre vægge, træk hjørner og vægge, eller skriv hjørnerne, væggenes længder og buer i panelet *Vægge*. Hallens mål er i meter; du kan også skrive f.eks. 450 mm.
- Tilføj runde og firkantede søjler, inventar, døre, nødudgange og spærrede områder. Markér noget for at ændre størrelsen med dets håndtag, og træk inden i det for at flytte det. Et fastgørelsesgitter, 0,1 m, medmindre du vælger et andet, gør rektangler lette at tegne.
- Døre bliver i deres væg. En markeret dør viser afstandene fra hjørnerne til åbningen, som du også kan skrive.
- Markér de døre, hvor moduler kan bæres ind og ud; de får en blå dobbeltpil. Nødudgange er grønne, med et nødudgangsskilt, der peger mod udgangen. Etiketter står altid øverst.
- Hallen viser sit gulvareal og sit brugbare gulvareal: uden søjler, inventar, områderne, der skal holdes frie foran døre, og spærrede områder.
- *Udskriv* udskriver hallen på en side i liggende format, med størrelsen og gulvarealerne i midten.
- Tal vises og skrives, som dit sprog skriver dem, f.eks. 12,5 på dansk og 12.5 på engelsk.
- En knap i øverste højre hjørne af hver tegning viser hele tegningen igen, også for modultegninger.

## Version 0.6.0
Udgivet 2026-11-16
- Nyt modul: ende B kan have en anden endeprofil end ende A, også af en anden standard i samme skala, for eksempel FREMO i den ene ende og MMM i den anden. Markér modulet som endevender, når en asymmetrisk endeprofil skifter side. En asymmetrisk endeprofil, som FREMO-H0-B96, spejles nu i ende B, så den høje side bliver på samme side af sporet, og to sådanne moduler passer uden afvigelser.
- Modultegninger viser typen af endeprofil, som DWG- og SKP-tegninger gør: endeprofilens form foldes ind i modulet ved hver ende, mørk, hvor profilen ligger over skinneoverkant, og lys, hvor den ligger under. Endeprofilens navn står langs hver ende, inde i modulet. Endernes betegnelser og pile tegnes ikke længere.
- Endeprofiler har et kort navn, for eksempel *B96*, som vises i modultegninger. Uden kort navn vises betegnelsen. De indbyggede FREMO-endeprofiler har de korte navne *B96*, *B96f* og *2E96*.
- Modultegninger fylder vinduet, med filhandlingerne ved siden af, og kan udskrives: *Udskriv* udskriver tegningen alene med modulets navn, tilpasset en liggende side.
- Et modul identificeres med ejerens FREMO-præfiks og et FREMO-nummer, for eksempel *SFjä 012*, med et navn eller med begge dele. Et FREMO-nummer kræver præfikset; uden nummer skal modulet have et navn. Ejerens fulde navn kan også angives; ejeren kan være en anden end dig. Angiv dem i *Nyt modul*, og ret dem i ruden *Egenskaber* ved siden af modultegningen. Appen viser FREMO-betegnelsen sammen med navnet.
- Modulets FREMO-betegnelse, for eksempel *SFjä 7900*, eller ellers dets navn, står på modulet, med ejerens navn i mindre tekst nedenunder. Teksten går langs sporet, centreret mellem enderne, over sporet, som du læser den.

## Version 0.5.0
Udgivet 2026-11-15
- Tegninger gemmes som almindelige JSON-filer, navngivet efter deres type, for eksempel *Station Nord.module.json* eller *FREMO-H0-B96.endprofile.json*. De kan åbnes i enhver JSON-editor. Filer, der er gemt med de tidligere filtyper (.tdmodule, .tdendprofile og så videre), kan stadig åbnes.
- Åbn og importér tager imod alle JSON-filer; appen læser typen fra indholdet. Andre JSON-filer i en mappe eller et ZIP-arkiv springes over.
- Indstilling for skala: vælg den skala, du arbejder i, så vises kun standarder og endeprofiler i den skala, sammen med dit land.

## Version 0.4.0
Udgivet 2026-11-15
- Siden Indstillinger, under Indstillinger i topmenuen: dit brugernavn, landet du arbejder i, og din API-nøgle til Modulregistret. Indstillingerne gemmes i din browser, også efter opdateringer, og gemmes aldrig i filer.
- Ændringslog: når du gemmer en tegning, registreres dit brugernavn og datoen i den, én linje pr. person med den seneste dato. Loggen vises i egenskabspanelet. Uden brugernavn registreres intet, og problemindikatoren viser en advarsel.
- Standarder, der kun gælder i nogle lande, som MMM i Sverige, vises kun, når du vælger et af de lande i indstillingerne; det samme gælder deres tilføjelser og endeprofiler. Tegninger, der bruger dem, åbnes som normalt. Dine egne standarder kan angive deres lande.
- Startsiden viser ikke længere versionsnummeret; det står i topmenuen.

## Version 0.3.0
Udgivet 2026-11-15
- Informationssiden er nu den første side, med afsnittene Generelt og Understøttede browsere. Startsiden med kladder og seneste filer findes under Start i topmenuen.
- Udgivelsesnoter i appen: klik på megafonen ved siden af versionsnummeret. De vises på dit sprog.
- Problemindikator i topmenuen: et grønt flueben, eller et rødt tegn med antallet af problemer i dine egne katalogposter (standarder, tilføjelser og undtagelser for pasning). Klik på et problem for at åbne posten og rette den.
- Vælg sprog med flag i topmenuen.
- Appen har et navn på hvert sprog: Modultegneren, Module Draw, Modulritaren og Modulzeichner.
- De tyske tekster bruger "du".
- Ikonerne virker også offline.

## Version 0.2.0
Udgivet 2026-11-15
- Første offentlige forhåndsversion, til afprøvning. Appen hedder nu **Modultegneren** (Module Draw på engelsk).
- En informationsside, når appen starter, som også åbnes fra "Modultegneren" i topmenuen.
- Editor til endeprofiler: tegn endeprofilens overkant efter en måltegning med linjer, buer og afrundinger (med den side, afrundingens centrum ligger på), placer spor og skruehuller, med automatiske mål fra skinneoverkant eller fra endeprofilens nederste venstre hjørne.
- Udskriv en endeprofil på en liggende A4-side, med mål i papirstørrelse og titlen "Standard Skala Betegnelse".
- Opret moduler fra skabeloner, med en ventemeddelelse mens modulet tegnes, og vis endeprofiler, moduler og haller.
- Katalog over standarder og endeprofiler (FREMO, MMM), med egne standarder, tilføjelser og undtagelser for pasning; importér endeprofiler fra filer.
- Fortsæt arbejdet: knapperne Gem, Redigér og Kassér kladden for hver kladde. Kladder gemmes automatisk i browseren; gem dine tegninger i filer for at beholde og dele dem.
- Virker offline og kan installeres som en app; tekster på engelsk, svensk, tysk, dansk og norsk.
- Kendte begrænsninger: editorerne til moduler, haller og hal-planer er ikke færdige endnu. Filformatet kan stadig ændres før version 1.0. Det fungerer bedst at gemme og åbne filer i Chrome eller Edge på en computer.

## Version 0.1.0
Udgivet 2026-11-13
- Første udrulning af appens grundskelet: kan installeres og virker offline.
