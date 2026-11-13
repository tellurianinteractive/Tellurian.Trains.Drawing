# Tellurian Trains Drawing

An *open source* progressive web app for drawing model railway **end profiles**, **modules** and **hall plans**
for module meetings, such as those of FREMO and MMM.

- **End profiles**: draw the cross-section of a module end and see whether and how two ends fit:
  bolted, clamped or not at all.
- **Modules**: draw the baseboard sections, the track plan, the exits with their end profiles,
  operator spaces, public sides and duck-under areas. A module can have several variants.
- **Hall plans**: draw the hall, then drag modules into it, join them end to end, and check
  joints, clearances, walking paths and operator spaces.

The app runs entirely in the browser and works offline. Your drawings stay on your device;
you share them as files. Module files will also be accepted by the
[Module Registry](https://moduleregistry.azurewebsites.net/).

#### Status
Under development. See the [specification](docs/Specification.md) and the
[implementation plan](docs/ImplementationPlan.md).

#### Technology
Blazor WebAssembly (AOT compiled) on .NET 11, hosted as a static web app on Azure.
