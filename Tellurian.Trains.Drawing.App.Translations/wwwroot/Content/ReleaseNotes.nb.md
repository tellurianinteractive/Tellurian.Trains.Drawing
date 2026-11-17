# Versjonsmerknader

## Versjon 0.7.0
Utgitt 2026-11-17
- Hallredigering: *Ny hall…* på startsiden starter med et rektangel. Tegn andre vegger, dra hjørner og vegger, eller skriv hjørnene, veggenes lengder og buer i panelet *Vegger*. Hallens mål er i meter; du kan også skrive f.eks. 450 mm.
- Legg til runde og firkantede søyler, inventar, dører, nødutganger og sperrede områder. Merk noe for å endre størrelsen med håndtakene, og dra inni det for å flytte det. Et festerutenett, 0,1 m hvis du ikke velger et annet, gjør det lett å tegne rektangler.
- Dører blir i veggen sin. En merket dør viser avstandene fra hjørnene til åpningen, som du også kan skrive.
- Merk dørene der moduler kan bæres inn og ut; de får en blå dobbeltpil. Nødutganger er grønne, med et nødutgangsskilt som peker mot utgangen. Etiketter står alltid øverst.
- Hallen viser gulvarealet og det brukbare gulvarealet: uten søyler, inventar, områdene som skal holdes frie foran dører, og sperrede områder.
- *Skriv ut* skriver ut hallen på en side i liggende format, med størrelsen og gulvarealene i midten.
- Tall vises og skrives slik språket ditt skriver dem, f.eks. 12,5 på norsk og 12.5 på engelsk.
- En knapp øverst til høyre i hver tegning viser hele tegningen igjen, også for modultegninger.

## Versjon 0.6.0
Utgitt 2026-11-16
- Ny modul: ende B kan ha en annen endeprofil enn ende A, også av en annen standard i samme skala, for eksempel FREMO i den ene enden og MMM i den andre. Merk modulen som endevender når en asymmetrisk endeprofil bytter side. En asymmetrisk endeprofil, som FREMO-H0-B96, speiles nå i ende B, slik at den høye siden blir på samme side av sporet og to slike moduler passer uten avvik.
- Modultegninger viser typen endeprofil slik DWG- og SKP-tegninger gjør: endeprofilens form brettes inn i modulen ved hver ende, mørk der profilen ligger over skinneoverkant, og lys der den ligger under. Endeprofilens navn står langs hver ende, inne i modulen. Endenes betegnelser og piler tegnes ikke lenger.
- Endeprofiler har et kortnavn, for eksempel *B96*, som vises i modultegninger. Uten kortnavn vises betegnelsen. De innebygde FREMO-endeprofilene har kortnavnene *B96*, *B96f* og *2E96*.
- Modultegninger fyller vinduet, med filhandlingene ved siden av, og kan skrives ut: *Skriv ut* skriver ut tegningen alene med modulens navn, tilpasset en liggende side.
- En modul identifiseres med eierens FREMO-prefiks og et FREMO-nummer, for eksempel *SFjä 012*, med et navn eller med begge deler. Et FREMO-nummer krever prefikset; uten nummer må modulen ha et navn. Eierens fulle navn kan også oppgis; eieren kan være en annen enn deg. Oppgi dem i *Ny modul*, og endre dem i ruten *Egenskaper* ved siden av modultegningen. Appen viser FREMO-betegnelsen sammen med navnet.
- Modulens FREMO-betegnelse, for eksempel *SFjä 7900*, eller ellers navnet, står på modulen, med eierens navn i mindre tekst under. Teksten går langs sporet, midt mellom endene, over sporet slik du leser den.

## Versjon 0.5.0
Utgitt 2026-11-15
- Tegninger lagres som vanlige JSON-filer, navngitt etter typen, for eksempel *Station Nord.module.json* eller *FREMO-H0-B96.endprofile.json*. De kan åpnes i hvilken som helst JSON-redigerer. Filer som er lagret med de tidligere filendelsene (.tdmodule, .tdendprofile og så videre), kan fortsatt åpnes.
- Åpne og importer tar imot alle JSON-filer; appen leser typen fra innholdet. Andre JSON-filer i en mappe eller et ZIP-arkiv hoppes over.
- Innstilling for skala: velg skalaen du arbeider i, så vises bare standarder og endeprofiler i den skalaen, sammen med landet ditt.

## Versjon 0.4.0
Utgitt 2026-11-15
- Siden Innstillinger, under Innstillinger i toppmenyen: brukernavnet ditt, landet du arbeider i, og API-nøkkelen din for Modulregisteret. Innstillingene lagres i nettleseren, også etter oppdateringer, og lagres aldri i filer.
- Endringslogg: når du lagrer en tegning, registreres brukernavnet ditt og datoen i den, én linje per person med den siste datoen. Loggen vises i egenskapspanelet. Uten brukernavn registreres ingenting, og problemindikatoren viser en advarsel.
- Standarder som bare gjelder i noen land, som MMM i Sverige, vises bare når du velger ett av disse landene i innstillingene; det samme gjelder tilleggene og endeprofilene deres. Tegninger som bruker dem, åpnes som vanlig. Dine egne standarder kan angi landene sine.
- Startsiden viser ikke lenger versjonsnummeret; det står i toppmenyen.

## Versjon 0.3.0
Utgitt 2026-11-15
- Informasjonssiden er nå den første siden, med avsnittene Generelt og Nettlesere som støttes. Startsiden med utkast og nylige filer finner du under Start i toppmenyen.
- Versjonsmerknader i appen: klikk på megafonen ved siden av versjonsnummeret. De vises på ditt språk.
- Problemindikator i toppmenyen: en grønn hake, eller et rødt tegn med antall problemer i dine egne katalogoppføringer (standarder, tillegg og unntak for tilpasning). Klikk på et problem for å åpne oppføringen og rette den.
- Velg språk med flagg i toppmenyen.
- Appen har et navn på hvert språk: Modultegneren, Module Draw, Modulritaren og Modulzeichner.
- De tyske tekstene bruker "du".
- Ikonene virker også uten nett.

## Versjon 0.2.0
Utgitt 2026-11-15
- Første offentlige forhåndsversjon, for utprøving. Appen heter nå **Modultegneren** (Module Draw på engelsk).
- En informasjonsside når appen starter, som også åpnes fra "Modultegneren" i toppmenyen.
- Redigering av endeprofiler: tegn endeplatens overkant etter en målsatt tegning med linjer, buer og avrundinger (med hvilken side avrundingens sentrum ligger på), plasser spor og skruehull, med automatiske mål fra skinnetopp eller fra platens nedre venstre hjørne.
- Skriv ut en endeprofil på en liggende A4-side, med mål i papirstørrelse og tittelen "Standard Skala Betegnelse".
- Opprett moduler fra maler, med en ventemelding mens modulen tegnes, og vis endeprofiler, moduler og haller.
- Katalog over standarder og endeprofiler (FREMO, MMM), med egne standarder, tillegg og unntak for tilpasning; importer endeprofiler fra filer.
- Fortsett arbeidet: knappene Lagre, Rediger og Forkast utkastet for hvert utkast. Utkast lagres automatisk i nettleseren; lagre tegningene dine i filer for å beholde og dele dem.
- Virker uten nett og kan installeres som en app; tekster på engelsk, svensk, tysk, dansk og norsk.
- Kjente begrensninger: redigeringen av moduler, haller og hallplaner er ikke ferdig ennå. Filformatet kan fortsatt endres før versjon 1.0. Lagring og åpning av filer fungerer best i Chrome eller Edge på en datamaskin.

## Versjon 0.1.0
Utgitt 2026-11-13
- Første utrulling av appens grunnstruktur: kan installeres og virker uten nett.
