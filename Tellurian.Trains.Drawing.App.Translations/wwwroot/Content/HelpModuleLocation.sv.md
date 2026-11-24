# Driftplats

En driftplats är det som en tidtabellsplanerare ser av modulen: en plats där tåg stannar, möts eller leds om. En modul utan driftplats är linje.

- **Gör till driftplats** gör modulen till en station med modulens namn. Ge den en signatur.
- **Ta bort driftplatsen** gör modulen till linje igen. Spåren blir kvar i rutan *Spår*.

## Egenskaperna

- **Namn** och **Signatur**: signaturen är det korta namn som används i tidtabeller, t.ex. *Tby*. Den är unik inom landet.
- **Land**: det land som driftplatsen trafikeras som en del av, som en kod med två bokstäver, t.ex. *SE*.
- **Typ**:
  - **Station**: tåg stannar för att mötas, bli förbigångna eller byta resande eller gods.
  - **Signalreglerad plats**: en blockpost, linjeplats eller mötesplats. Tåg stannar aldrig där.
  - **Industriområde**: bara gods.
  - **Annan plats**: t.ex. en hållplats. Bara resande.
- Kryssrutorna anger vad som kan göras på platsen. Det som typen inte kan ha är avstängt, och det som den alltid har är påslaget. När du byter typ får du en fråga innan något tas bort.
- **Tåg kan mötas** väljs för en signalreglerad plats. På en station kan tåg mötas när den har mer än ett tidtabellslagt spår.
- **Magasin**: en station som står för världen utanför banan. Där sker alltid utbyte av resande och gods.
- **Mittpunkt**: avstånd till andra driftplatser mäts från mittpunkten, t.ex. stationshuset, längs huvudspåret. Den ligger mitt på modulen tills du avmarkerar **Mittpunkt mitt på modulen** och anger avståndet från vänster ände.

Problem visas i rött men hindrar dig inte. Ändringar kan ångras.
