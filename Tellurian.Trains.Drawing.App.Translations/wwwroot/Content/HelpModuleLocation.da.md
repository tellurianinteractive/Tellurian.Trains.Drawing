# Driftssted

Et driftssted er det, en køreplanlægger ser af modulet: et sted, hvor tog standser, krydser eller ledes. Et modul uden driftssted er fri bane.

- **Gør til driftssted** gør modulet til en station med modulets navn. Giv den en signatur.
- **Fjern driftsstedet** gør modulet til fri bane igen. Sporene bliver i ruden *Spor*.

## Egenskaberne

- **Navn** og **Signatur**: signaturen er det korte navn, der bruges i køreplaner, f.eks. *Tby*. Den er unik inden for landet.
- **Land**: det land, driftsstedet drives som en del af, som en kode på to bogstaver, f.eks. *SE*.
- **Type**:
  - **Station**: tog standser for at krydse, blive overhalet eller udveksle passagerer eller gods.
  - **Signalstyret sted**: en blokpost, et forgreningssted eller et krydsningssted. Tog standser aldrig der.
  - **Industriområde**: kun gods.
  - **Andet sted**: f.eks. et trinbræt. Kun passagerer.
- Afkrydsningsfelterne siger, hvad der kan ske på stedet. Det, typen ikke kan have, er slået fra, og det, den altid har, er slået til. Når du skifter type, bliver du spurgt, før noget fjernes.
- **Tog kan krydse** vælges for et signalstyret sted. På en station kan tog krydse, når den har mere end ét spor i køreplanen.
- **Skyggebanegård**: en station, der står for verden uden for anlægget. Der udveksles altid passagerer og gods.
- **Midtpunkt**: afstande til andre driftssteder måles fra midtpunktet, f.eks. stationsbygningen, langs hovedsporet. Det ligger midt på modulet, indtil du fjerner markeringen i **Midtpunkt midt på modulet** og angiver afstanden fra venstre ende.

Problemer vises med rødt, men stopper dig ikke. Ændringer kan fortrydes.
