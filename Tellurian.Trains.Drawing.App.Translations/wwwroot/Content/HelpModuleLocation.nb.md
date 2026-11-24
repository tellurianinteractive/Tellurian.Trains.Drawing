# Driftssted

Et driftssted er det en ruteplanlegger ser av modulen: et sted der tog stopper, krysser eller ledes. En modul uten driftssted er fri linje.

- **Gjør til driftssted** gjør modulen til en stasjon med modulens navn. Gi den en signatur.
- **Fjern driftsstedet** gjør modulen til fri linje igjen. Sporene blir stående i ruten *Spor*.

## Egenskapene

- **Navn** og **Signatur**: signaturen er det korte navnet som brukes i ruteplaner, f.eks. *Tby*. Den er unik innenfor landet.
- **Land**: landet driftsstedet drives som en del av, som en kode på to bokstaver, f.eks. *SE*.
- **Type**:
  - **Stasjon**: tog stopper for å krysse, bli forbikjørt eller utveksle passasjerer eller gods.
  - **Signalstyrt sted**: en blokkpost, et forgreningssted eller et kryssingssted. Tog stopper aldri der.
  - **Industriområde**: bare gods.
  - **Annet sted**: f.eks. en holdeplass. Bare passasjerer.
- Avkrysningsboksene sier hva som kan skje på stedet. Det typen ikke kan ha, er slått av, og det den alltid har, er slått på. Når du bytter type, blir du spurt før noe fjernes.
- **Tog kan krysse** velges for et signalstyrt sted. På en stasjon kan tog krysse når den har mer enn ett spor i ruteplanen.
- **Skyggestasjon**: en stasjon som står for verden utenfor anlegget. Der utveksles det alltid passasjerer og gods.
- **Midtpunkt**: avstander til andre driftssteder måles fra midtpunktet, f.eks. stasjonsbygningen, langs hovedsporet. Det ligger midt på modulen til du fjerner krysset i **Midtpunkt midt på modulen** og oppgir avstanden fra venstre ende.

Problemer vises i rødt, men stopper deg ikke. Endringer kan angres.
