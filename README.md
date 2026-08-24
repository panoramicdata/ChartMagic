[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

[![NuGet version](https://img.shields.io/nuget/v/PanoramicData.ChartMagic.svg)](https://www.nuget.org/packages/PanoramicData.ChartMagic/)

[![Codacy Badge](https://app.codacy.com/project/badge/grade/PanoramicData.ChartMagic)](https://app.codacy.com/gh/panoramicdata/PanoramicData.ChartMagic/dashboard)

# ChartMagic
An open source, charting nuget package.  Renders to vector and bitmap formats.

## The demo

`PanoramicData.ChartMagic.Demo` is a WebAssembly page showing one chart per tab, with every
property of the `ChartSpecification` editable beside it. It is published from `main` to
<https://panoramicdata.github.io/PanoramicData.ChartMagic/>.

### Comparing against a DocMagic server (local development only)

The demo can render each sample a second time on a Windows DocMagic server and show the two one
above the other, which is how this library's output is checked against the renderer it replaces.

This only ever works when the demo is served from `localhost`. The published site does not offer
it, and does not read or write the settings behind it — an API key must not end up in the browser
storage of a public origin. That is enforced in code, not by convention.

A browser cannot call the chart endpoint directly. The API key has to go in a header, which makes
the request preflighted, and the endpoint answers a preflight with `401` and no cross-origin
headers at all. So a small relay runs on your machine and forwards the request server-side:

```powershell
# In one terminal - listens on loopback only, and holds no configuration of its own
dotnet run tools/DocMagicRelay.cs

# In another
dotnet run --project PanoramicData.ChartMagic.Demo
```

Then open the demo with the server and key in the query string, once:

```text
http://localhost:5227/?docmagic=http://your-docmagic:8080&key=YOUR_API_KEY
```

They are stored in browser storage and removed from the address bar, so the key is not left in a
URL to be copied or bookmarked. A **Forget** control clears them. The key is never displayed.

Each sample then has a **Render** button that draws it on the server and shows the result beneath
this library's SVG. Rendering is per sample and on demand: the corpus is thirty-odd charts, and
each one is a round trip.

Add `&relay=http://localhost:1234` if the relay is on a different port.

#### What is and is not sent

`DocMagicRequest` projects a `ChartSpecification` into the request body. The two specifications
describe the same thing and were named to match, but only 72 of their 114 properties share a name,
their vertical positions run in opposite directions, and the wire format is not what the endpoint's
own client writes:

- colours go as hex, not as the `R, G, B` form that client emits
- enumerations go as **numbers**, and not this library's numbers — `Column` is 9 there and 0 here

A handful of properties have no counterpart and are listed in `DocMagicRequest.NotSent` with the
reason, so a difference caused by something simply not being sent is not mistaken for a rendering
difference. An enumeration member with no known value throws rather than defaulting: a comparison
rendered from a wrong value is worse than no comparison, because it reads as a rendering difference.
