# استراتژی تست

پروژه `Tests` به ازای هر لایه پوشه اختصاصی دارد و روی xUnit با الگوی «تست واحد بدون وابستگی شبکه +
تست یکپارچه روی PostgreSQL واقعی با Testcontainers» کار می‌کند. موتور کانتینر تست، **Podman** است
(با سازگاری Docker) و به‌صورت خودکار شناسایی می‌شود. این سند ساختار، زیرساخت مشترک
(`TestInfrastructure`)، آمار واقعی فایل‌ها و نحوه اجرای تست‌ها را مستند می‌کند.

## ساختار و آمار

شمارش با شمارش فایل‌سیستم (بدون bin/obj) انجام شده است:

| پوشه | تعداد فایل .cs | از این تعداد `*Tests.cs` |
|---|---|---|
| `Tests/Application` | ۵۳۰ | ۵۳۰ |
| `Tests/Infrastructure` | ۲۵۴ | ۲۵۳ (+ ۱ فایل کمکی `SkippableTheoryAttribute.cs`) |
| `Tests/Domain` | ۱۹۰ | ۱۹۰ |
| `Tests/Presentation` | ۱۵۲ | ۱۵۲ |
| `Tests/TestInfrastructure` | ۷۲ | ۲ (تست‌های `ContainerEngine`) |
| `Tests/SharedKernel` | ۳۲ | ۳۲ |
| `Tests/Integration` | ۱۱ | ۱۱ |
| ریشه Tests (`Imports.cs`) | ۱ | ۰ |
| **جمع** | **۱٬۲۴۲** | **۱٬۱۷۰** |

بزرگ‌ترین نواحی تست Application به ترتیب: Wallet (۷۴)، Review (۴۶)، Order (۳۸)، Inventory (۳۵)، Payment (۲۵)، Product (۲۴)، Auth (۲۱).

## فریم‌ورک‌ها و پکیج‌ها

| پکیج | نسخه | نقش |
|---|---|---|
| `xunit` + `xunit.runner.visualstudio` | 2.9.2 / 2.8.2 | فریم‌ورک و رانر |
| `Shouldly` | 4.3.0 | Assertion (بدون FluentAssertions) |
| `NSubstitute` | 6.0.0 | Mock و Stub |
| `Bogus` | 35.6.5 | تولید داده تستی |
| `Testcontainers.PostgreSql` | 4.16.0 | کانتینر PostgreSQL واقعی روی Podman/Docker |
| `Respawn` | 7.0.0 | ریست دیتابیس بین تست‌ها |
| `Xunit.SkippableFact` | 1.5.61 | ساخت Skip پویا (جزئیات در بخش نکات) |
| `coverlet.collector` | 10.1.0 | پوشش کد |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.13 | ارجاع شده اما استفاده نمی‌شود — هیچ `WebApplicationFactory` یا تست HTTP در ریپو وجود ندارد |

## تست واحد به تفکیک لایه

| لایه | تمرکز | مثال واقعی |
|---|---|---|
| Domain | رفتار Aggregateها و ValueObjectها | `Tests/Domain/Brand/Aggregates/BrandTests.cs` — تست `Brand.Create` با `BrandBuilder` و `StubBrandUniquenessChecker` (کنترل `CallCount`) |
| Application | هندلرها و Validatorها | `Tests/Application/Brand/Features/Commands/CreateBrand/CreateBrandHandlerTests.cs` — Mockهای NSubstitute از `IBrandRepository` و ... و کنترل مسیرهای NotFound/Conflict با `ShouldFailWith` |
| Infrastructure | ریپازیتوری‌ها و سرویس‌ها (واحد) | تست‌های `Tests/Infrastructure` روی کلاس‌های ایزوله |
| Presentation | Middleware، فیلترها، ProblemDetails، Swagger | ۱۵۲ فایل تست واحد با NSubstitute |
| SharedKernel | Guard، Result، Specification و ابزارها | ۳۲ فایل |
| TestInfrastructure | منطق موتور کانتینر (تشخیص Podman، endpoint، Ryuk) | `Tests/TestInfrastructure/ContainerEngine/ContainerEngineResolverTests.cs` و `PodmanProbeTests.cs` |

الگوی رایج: به ازای هر Feature دو فایل `*HandlerTests` و `*ValidatorTests` وجود دارد.

## TestInfrastructure

زیرساخت مشترک در `Tests/TestInfrastructure` با پوشه‌های زیر:

| پوشه | محتوا |
|---|---|
| `Base/` | `IntegrationTestBase` — پایه `IAsyncLifetime` که `PostgresContainerFixture` را می‌گیرد، بدون موتور کانتینر `Skip.IfNot` می‌زند، `DBContext` واقعی می‌سازد و Seed helperهای `SeedCategoryAsync`/`SeedBrandAsync`/`SeedUserAsync` دارد |
| `Builders/` | ۴۹ فایل Builder روان برای Aggregate و VO: `BrandBuilder`، `CategoryBuilder`، `ProductBuilder`، `OrderBuilder`، `WalletBuilder` و ... |
| `Fakes/` | Fakeهای رفتاری: `FakeElasticsearchServer` (سرور جعلی TCP با Router پاسخ)، `FakeHttpMessageHandler` (HttpClient اسکریپت‌پذیر)، `FakeLockHandle`، `FakeOrderPaymentContext` |
| `Stubs/` | Stubهای دستی با شمارش فراخوانی: `StubBrandUniquenessChecker`، `StubCategoryUniquenessChecker`، `StubAttributeTypeUniquenessChecker` |
| `Assertions/` | `ServiceResultAssertions` — اکستنشن‌های Shouldly مثل `ShouldBeSuccess` و `ShouldFailWith(errorCode)` |
| `Fixtures/` | `AutoMapperFixture` — نامش میراثی است؛ در واقع `TypeAdapterConfig` مپستر را Scan و `IMapper` می‌سازد |
| `Mapping/` | `MapsterConfigFixture` برای تست‌های نگاشت |
| `Helpers/` | `CatalogSeeder` — Seed دسته/برند/محصول واقعی در `DBContext` |
| `Database/` | `PostgresContainerFixture` (Testcontainers با `postgres:16-alpine`، کاربر/پایگاه `mechanic/mechanic_tests`) و `DatabaseCollection` |
| `ContainerEngine/` | `ContainerEngineBootstrap` (تشخیص خودکار Podman در زمان لود اسمبلی)، `ContainerEngineResolver` (انتخاب endpoint)، `PodmanProbe` (خواندن `podman machine inspect`/`podman info`/`system connection list`)، `ContainerEngineEndpointProbe` (تست دسترس‌پذیری endpoint) |

## تست یکپارچه

- سطح تست، **ریپازیتوری/Persistence** است نه HTTP: ریپازیتوری واقعی Infrastructure روی `DBContext` واقعی
  (با همان `AuditableEntityInterceptor` و `DomainEventInterceptor`) اجرا می‌شود.
- کانتینر PostgreSQL (`postgres:16-alpine`) به ازای هر اجرا یک بار بالا می‌آید
  (`[Collection(nameof(DatabaseCollection))]`) و بعد از هر تست با Respawn (اسکیمای `public`) ریست می‌شود.
- الگوی مشخصه: `[Trait("Category", "Integration")]`؛ نمونه: `Tests/Integration/Order/OrderStatusRepositoryIntegrationTests.cs`.
- تست‌های این بخش با `[SkippableFact]`/`[SkippableTheory]` علامت‌گذاری شده‌اند؛ بدون موتور کانتینر،
  فیکسچر `SkipException` با پیام `No container engine reachable...` می‌سازد و نتیجه در Visual Studio و `dotnet test`
  به‌صورت **Skipped** گزارش می‌شود (نه Failed).

## موتور کانتینر تست (Podman)

Testcontainers با API سازگار Docker صحبت می‌کند؛ Podman همین API را از طریق **named pipe** (ویندوز) یا
**unix socket** (لینوکس/مک) ارائه می‌دهد. `ContainerEngineBootstrap` یک `[ModuleInitializer]` است که پیش از
هر مصرف Testcontainers اجرا می‌شود و در صورت نبود `DOCKER_HOST`، Podman را کشف و پیکربندی می‌کند.
کاندیدها (pipe ماشین، pipe مبتنی بر نام connection و سوکت‌ها) با یک probe کوتاه بررسی می‌شوند؛ اگر pipe
Podman برای پروسهٔ تست باز نشود (مثلاً VS غیر-elevated)، `docker_engine` (Podman Desktop با Docker Compatibility
یا Docker Desktop) به‌عنوان fallback امتحان می‌شود. اگر هیچ endpointی پاسخ ندهد، همان endpoint اول Podman
برای پیام تشخیصی انتخاب می‌شود و تست‌ها Skip می‌شوند.

| حالت | endpoint | نکته |
|---|---|---|
| ویندوز + `podman machine` در حال اجرا | `npipe://./pipe/<machine>` (از `podman machine inspect`) | rootless ⇒ `TESTCONTAINERS_RYUK_DISABLED=true` |
| ویندوز در محیط محدود (lockfile قابل نوشتن نیست) | همان pipe از نام default در `podman system connection list` | فallback طراحی‌شده؛ `DOCKER_HOST` ست می‌شود |
| لینوکس/مک با machine | `unix://<PodmanSocket.Path>` | — |
| Podman بومی لینوکس (بدون machine) | `unix://<remoteSocket.path>` از `podman info` | rootless ⇒ Ryuk خاموش |
| Docker (یا Podman در دسترس نیست) | رفتار پیش‌فرض Testcontainers | بدون تغییر |

### متغیرهای محیطی

| متغیر | پیش‌فرض | کاربرد |
|---|---|---|
| `MECHANIC_CONTAINER_ENGINE` | `auto` | `podman` = فقط Podman (اسکریپت `run-tests.ps1` در نبودش خطا می‌دهد؛ تشخیص خودکار تست‌ها در نبود endpoint به Docker برمی‌گردد)، `docker` = نادیده‌گرفتن Podman |
| `PODMAN_PATH` | PATH / `%ProgramFiles%\RedHat\Podman\podman.exe` | مسیر صریح اجرایی Podman |
| `DOCKER_HOST` | — | اگر ست باشد، تشخیص خودکار **هرگز** آن را بازنویسی نمی‌کند (برای CI/remote) |
| `TESTCONTAINERS_RYUK_DISABLED` | برای rootless خودکار `true` | Ryuk با Podman rootless نیاز به mount سوکت دارد |
| `TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE` | — | فقط اگر می‌خواهید Ryuk با socket غیراستاندارد فعال بماند (مثلاً `/run/podman/podman.sock` در CI) |

### اجرا

```powershell
# اسکریپت پروژه: موتور را صریح pin می‌کند (پیش‌فرض auto)
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/testing/run-tests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/testing/run-tests.ps1 -Engine podman -Filter "FullyQualifiedName~Wallet"

# dotnet test معمولی هم Podman را خودکار تشخیص می‌دهد
dotnet test
dotnet test --filter Category=Integration
```

پیش‌نیاز Podman:

```powershell
podman machine init      # فقط بار اول
podman machine start     # باید running باشد
podman version
```

### CI

job `build-test` در `.github/workflows/ci.yml` روی runner اوبونتو Podman را نصب و سرویس rootful را
روشن می‌کند:

```yaml
env:
  DOCKER_HOST: unix:///run/podman/podman.sock
  TESTCONTAINERS_RYUK_DISABLED: 'true'
  MECHANIC_CONTAINER_ENGINE: podman
steps:
  - run: |
      sudo apt-get update && sudo apt-get install -y podman
      sudo systemctl enable --now podman.socket
      sudo chmod 666 /run/podman/podman.sock
      sudo podman pull postgres:16-alpine
```

سوکت rootful است تا محدودیت‌های user-namespace و Ryuk وجود نداشته باشد؛ Ryuk به‌دلیل تفاوت مسیر سوکت
داخل کانتینر خاموش است.

### رفع اشکال

| نشانه | علت | راه‌حل |
|---|---|---|
| `Access to the path is denied` هنگام نوشتن در پوشه‌های موقت تست‌ها (نمونهٔ `audit-archive-tests`, `dotenv-loader-tests`, `dbfactory-tests`) | پروسهٔ تست (testhost) در **سطح دسترسی Low** اجرا می‌شود (رفتار خود vstest) و در `%TEMP%` (برچسب Medium) اجازهٔ ساخت پوشه ندارد | `Setup-PodmanTestcontainers.ps1` پوشهٔ `%TEMP%\Low` را با برچسب low می‌سازد؛ هلپر `TestTempDirectory` تست‌ها به‌صورت خودکار به آن مسیر برمی‌گردد (اگر وجود نداشت، `%TEMP%` استفاده می‌شود) |
| `Access to the path is denied` یا `No container engine reachable` هنگام اتصال به `npipe://./pipe/podman-machine-default` (نمونه: Visual Studio) | پروسهٔ تست (testhost) در سطح دسترسی Low است و پایپ‌های Podman (سرویس win-sshproxy معمولاً High) را read-write نمی‌کند؛ پایپ `docker_engine` هم اگر سطح بالاتری داشته باشد همان خطا را می‌دهد | یک بار `Setup-PodmanTestcontainers.ps1` را اجرا کنید: سرویس API پادمن را روی `tcp://localhost:2375` روشن می‌کند (بدون محدودیت integrity؛ bootstrap خودکار آن را probe و pin می‌کند). سپس `Podman.runsettings` را در VS انتخاب کنید؛ ۱) VS را با همان سطح دسترسی Podman (Run as Administrator) اجرا کنید؛ ۲) `podman machine stop; podman machine start`؛ ۳) تشخیص دقیق: `scripts/testing/diagnose-podman.ps1` |
| `creating lockfile for VM: ... access denied` از `podman machine inspect` | پروسه اجازهٔ نوشتن در `%USERPROFILE%\.config\containers` را ندارد | fallback خودکار روی `podman system connection list` انجام می‌شود؛ یا `PODMAN_PATH`/`DOCKER_HOST` را صریح بدهید |
| `podman machine start` خطا می‌دهد | WSL/machine نصب یا healthy نیست | `podman machine init`، سپس `podman machine start`؛ در لینوکس `systemctl --user enable --now podman.socket` |
| تست‌ها با پیام «No running Podman» اسکیپ می‌شوند | machine خاموش است یا Podman پیدا نشد | `podman machine start`؛ در صورت نصب غیراستاندارد `-PodmanPath` |
| `image pull` کند/مسدود | دسترسی registry | `podman pull postgres:16-alpine` را قبل از تست اجرا کنید (CI همین کار را می‌کند) |

## اجرای تست‌ها

```bash
dotnet test                                     # همه تست‌ها (Podman خودکار)
dotnet test --filter Category=Integration       # فقط تست‌های یکپارچه (نیازمند Podman/Docker)
dotnet test --collect:"XPlat Code Coverage"     # پوشش با coverlet
```

پیش‌نیاز خاصی جز Podman (فقط برای تست‌های دیتابیسی) وجود ندارد؛ فایل appsettings یا runner config
جداگانه‌ای برای تست‌ها نیست و پوشش کد آستانه (Threshold) الزام‌آور ندارد.

## نکات و ناهماهنگی‌های فعلی

| مورد | توضیح |
|---|---|
| `Tests.csproj.Backup.tmp` | فایل بکاپ قدیمی در ریشه Tests باقی مانده و بخشی از build نیست |
| تست‌های کانتینری | فایل قدیمی `SkippableTestFrameworkSetup.cs` (ارجاع به پکیج ناموجود `Xunit.RequiresDockerFact`) حذف شد؛ همهٔ تست‌های وابسته به موتور کانتینر اکنون `[SkippableFact]`/`[SkippableTheory]` هستند و نبود موتور به **Skipped** منجر می‌شود |
| نبود Fake زمان | فیکه مثل `FakeDateTimeProvider` وجود ندارد؛ تست‌های وابسته به زمان از Builder/Seed استفاده می‌کنند |
