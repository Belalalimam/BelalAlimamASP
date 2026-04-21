//# NakhlaBelal — NewTess Parity Roadmap

//> Reference: [shop.newtess.com] (https://shop.newtess.com/)
//> Goal: bring NakhlaBelal's **infrastructure / feature set** up to newtess parity.
//> Scope: backend data model, taxonomies, product pages, filter sidebar, commerce features — **not visual design**.

//---

//## 1. What NewTess has

//### 1.1 Taxonomies (5 independent filter axes)

//| Axis | Count | NakhlaBelal status |
//|---|---|---|
//| By Type (Velvet, Jacquard, Lace, Tartan, Boucle, Brocade, Coating, Fine Suiting, Organic, Plain, Print, Shirting) | 12 | ✅ `FabricType` |
//| By Project (Dress, Coat, Bridal, Menswear, Suiting, Skirt, Jacket, Light Coat, Lingerie, Beach &Swimwear, Party &Evening, Blouse, Couture, Pants, Formal Wear) | 15 | ✅ `ProjectCategory` |
//| By Composition (Silk, Wool, Cotton, Linen, Viscose/Rayon, Cashmere, Polyester &Synthetic, Stretch) | 8 | ✅ `Composition` |
//| By Color (Beige, Black, Blue, Brown, Fuchsia, Gray, Green, Orange, Pink, Purple, Red, Silver, White, Yellow) | 14 | ✅ `Color` |
//| **By Pattern** (Abstract, Animal, Checks &Plaid, Ethnic, Floral, Geometric, Glen Plaid, Herringbone, Houndstooth, Pinstripe, Polka Dot, Stripes, Tie) | 13 | ❌ **MISSING** |

//### 1.2 Product card fields (listing)

//```
//[Image]
//Silk Satin Stretch 000009        ← name + internal SKU
//94 % SE, 6 % EA                    ← composition percentages(ISO abbrev)
//140 cm                           ← Width
//97 g / m                         ← Weight
//Premium                          ← Quality tier
//27.7 m                           ← Stock in meters (decimal)
//€ 53.15 / m + VAT                ← per-meter price
//```

//### 1.3 Product detail spec table

//| Field | Example | NakhlaBelal |
//|---|---|---|
//| Composition | 82% VI, 18% SE | ✅ (many-to-many with %) |
//| Height (useful width) | 135 cm (useful 130 cm) | ⚠️ have `Width`, missing `UsableWidth` |
//| Weight | 235 g / m | ✅ `Weight` |
//| Project | Dress, Pants, Shirt, Skirt | ✅ `ProjectCategories` |
//| **Care Instructions** | Do not wash / Do not bleach / Gentle HC clean / No tumble / Iron low | ❌ missing |
//| **Article code** | T01527 | ❌ missing (only DB Id) |
//| Color | 043 (Pink) | ✅ `Color` |
//| **Usage** | Womenswear / Menswear / Beachwear | ❌ missing |
//| **Dyeing Type** | Piece Dyed / Yarn Dyed / Print | ❌ missing |

//### 1.4 Commerce features

//- **Bulk tier pricing**: "Save 8% — buy 8 m or more at €48.90/m"
//- **Decimal quantity in meters** (15.6 m, 27.7 m — not integer pieces)
//- **Live stock** shown on card + detail
//- **Shipping promise** label above Add-to-cart ("Ships in 2 business days")
//- **Reviews** with verified-purchase flag + customer location + date
//- **Related products grouped by 4 facets** (same Type / same Color / same Composition / same Pattern)
//- **Sample request** as standalone page `/request-fabric-samples/`
//- **Multi-language** URL prefixes `/en/`, `/fr/`, `/it/`

//### 1.5 URL taxonomy pattern

//```
///en/fabric-type/velvet/
///en/fabrics-composition/silk/
///en/fabrics-color/pink/
///en/fabrics-pattern/floral/
///en/fabrics-used-for/dress/
///en/product-tag/new- arrivals /
/// en / fabrics /{ product - slug}/
//```

//---

//## 2. Gap Analysis (what NakhlaBelal needs to add)

//### 🔴 High priority — missing concepts

//| # | Feature | Proposed implementation |
//| ---| ---| ---|
//| 1 | **Pattern taxonomy * * | New `Pattern` entity + migration + admin CRUD (mirror `Color`) |
//| 2 | **Usage field** | Enum on `Product` (`Womenswear / Menswear / Beachwear / Home / Unisex`) |
//| 3 | **Dyeing Type** | Enum on `Product` (`PieceDyed / YarnDyed / Print / Plain`) |
//| 4 | **Care Instructions** | Bit-flags enum or `ICollection < ProductCareInstruction >` join — wash/bleach/dry-clean/tumble/iron-temp |
//| 5 | **Usable Width** | `UsableWidth decimal?` on `Product` (useful < loom width) |
//| 6 | **Article/Internal SKU** | `ArticleCode string?` on `Product` for internal search |
//| 7 | **Quality Tier * * | Enum on `Product` (`Premium / Standard / Outlet`) |
//| 8 | **Decimal stock in meters** | Change `StockQuantity` to `decimal(9,2)`; `CartItem.Quantity` too |
//| 9 | **Bulk pricing tiers** | `ProductPriceTier { Id, ProductId, MinQuantity, UnitPrice, DiscountPercent }` |
//| 10 | **Product reviews * * | `ProductReview { Id, ProductId, UserId, Rating, Title, Body, IsVerifiedPurchase, CreatedAt }` |

//### 🟡 Medium — exists but needs upgrade

//| # | Current | Upgrade |
//| ---| ---| ---|
//| 11 | Basic product card | Show composition% + width + weight + stock(m) + price/m |
//| 12 | Simple "related products" | Split into 4 facet-grouped blocks |
//| 13 | Single-select filters | Multi-select on 5 axes + URL querystring standard + active-chip removal |
//| 14 | Generic shipping info | "Ships in N business days" label above Add-to-cart |
//| 15 | No sample request | `/Customer/Home/RequestSamples` page + form + email |

//### 🟢 Low — SEO / UX polish

//| # | Item |
//|---|---|
//| 16 | Slug-based URLs for taxonomies (`/fabric-type/{slug}` instead of `?FabricTypeId=1`) |
//| 17 | Unified breadcrumb component on listing + detail |
//| 18 | Composition abbreviation legend/tooltip (SE, VI, CO, WO, EA, PL, PU, LI, CA, WS) |
//| 19 | Glossary + blog static pages |
//| 20 | Per - product translations(Name_ar / Name_en, Description_ar / Description_en) |

//---

//## 3. Implementation phases

//### Phase 1 — Data model foundation

//- [] Create `Pattern` entity (Id, Name, Slug, Description, ImageUrl, ICollection<Product>).
//- [ ] Add fields to `Product`:
//  - [] `UsableWidth decimal?`
//  - [ ] `ArticleCode string?`
//  - [ ] `QualityTier` enum (Premium / Standard / Outlet)
//  - [] `Usage` enum (Womenswear / Menswear / Beachwear / Home / Unisex)
//  - [] `DyeingType` enum (PieceDyed / YarnDyed / Print / Plain)
//  - [] `PatternId` FK → `Pattern`
//  - [ ] `ShippingLeadDays int?`
//- [ ] Change `StockQuantity` (and `CartItem.Quantity`, `OrderItem.Quantity`) from `int` → `decimal(9,2)`.
//- [ ] New `ProductCareInstruction` entity OR bit-flags enum (`NoWash`, `GentleWash`, `NoBleach`, `DryClean`, `NoTumble`, `IronLow`, `IronMedium`, `IronHigh`).
//- [ ] New `ProductPriceTier { Id, ProductId, MinQuantity, UnitPrice, DiscountPercent }`.
//- [] EF migration(s) +seed data.
//- [] Admin CRUD pages for `Pattern` and `ProductPriceTier` (tier editor on Product edit page).

//### Phase 2 — Customer-facing display

//- [ ] Update `_ProductCard.cshtml` — show composition%, width, weight, stock(m), price/m.
//- [ ] Update `Details.cshtml` spec table — add Usable Width, Article Code, Quality Tier, Usage, Dyeing Type, Pattern, Care Instructions (as icons).
//- [ ] Add price-tier table on detail page; dynamic cart pricing that auto-applies tier discount when qty ≥ MinQuantity.
//- [ ] Filter sidebar — add Pattern, Usage, DyeingType, QualityTier, Width range, Weight range, In-stock - only toggle; multi - select.
//- [] Related - products split into 4 facet-grouped blocks (same Type / same Color / same Composition / same Pattern).
//- [ ] Live stock label + "Ships in N business days" on detail page.

//### Phase 3 — Commerce features

//- [ ] Sample request: page + `SampleRequestController` + `SampleRequest` entity + email notification.
//- [ ] Product reviews: submission form(guarded by "bought this product" check against `Order`/`OrderItem`), moderation flag, star-rating aggregate on card + detail.
//- [ ] Decimal - quantity cart: update cart UI to allow e.g. 2.5 m input, recompute totals correctly.
//- [ ] Bulk - tier discount applied at cart line level.

//### Phase 4 — SEO / UX polish

//- [ ] Slug - based URLs for each taxonomy (route: `/ Customer / Home / FabricType /{ slug}`, etc.) — replace current `?FabricTypeId=1` style.
//- [ ] Unified `_Breadcrumbs.cshtml` partial used by listing + detail + taxonomy pages.
//- [ ] Composition abbreviation legend in a tooltip / glossary footer.
//- [ ] Glossary + blog static pages (`/glossary`, `/blog/{slug}`).
//- [] Per - product AR / EN translations — shadow columns or a `ProductTranslation` join entity.

//---

//## 4. Decisions to make before coding

//1. **Care instructions** → bit-flags enum vs join entity?
//   - Bit-flags: simpler schema, faster queries, harder to extend.
//   - Join entity: admin - editable list, matches the pattern of other taxonomies.
//   - **Recommendation:**join entity(`CareInstruction` lookup + `ProductCareInstruction` many - to - many).Consistent with existing pattern.

//2. **Decimal quantity** → globally, or only for "sold by meter" products?
//   - Global change is cleaner but invasive (CartItem, OrderItem, Inventory all shift).
//   - Per-product `SoldByMeter bool` flag + keep int for buttons/badges is hybrid.
//   - **Recommendation:**hybrid — add `Product.SoldByMeter bool`, use `decimal` everywhere but round to int when `!SoldByMeter`.

//3. **Tier pricing** → percentage discount or fixed unit price?
//   - NewTess shows both ("Save 8% — at €48.90/m"). Store percent + computed price.
//   - **Recommendation:** store `DiscountPercent` primary, derive unit price.

//4. **Reviews** → self-moderated or admin-approved?
//   - **Recommendation:** `IsApproved bool` default false, admin toggle; verified-purchase shown prominently.

//5. **URL slugs** → keep current id-based as fallback, or redirect?
//   - **Recommendation:** add slug route, 301-redirect legacy id routes.

//---

//## 5. Already-done prerequisites (done this session)

//- ✅ Adaptive product detail (hides fabric-only rows for non-fabric items)
//- ✅ Admin CRUD for Color, Composition, FabricType, ProductTag, ProjectCategory
//- ✅ Sidebar navigation section "Filters & Attributes" in admin
//- ✅ Homepage filter sections capped at 10 items (Colors / FabricTypes / ProjectCategories)

//---

//## 6. Rough effort estimate

//| Phase | Files touched | Migrations | Effort |
//|---|---|---|---|
//| 1 — Data model | ~15 (Models, DbContext, Admin controllers/views) | 2–3 | **L** (2 days) |
//| 2 — Display | ~10 (partials, Details, filter sidebar, HomeController) | 0 | **L** (2 days) |
//| 3 — Commerce | ~12 (new controllers, entities, cart logic, email) | 2 | **XL** (3 days) |
//| 4 — Polish | ~8 (routing, breadcrumbs, translations) | 1 | **M** (1.5 days) |

//**Total: ~8.5 days of focused work** (assuming no refactors of existing logic break).

//---

//_Last updated: 2026-04-21_
