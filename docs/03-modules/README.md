# فهرست ماژول‌های بیزینسی

این بخش مرجع مستندات ماژول‌های بیزینسی Mechanic API است: ۲۲ سند که هر یک فقط جزئیات همان ماژول —
دامنه، Application، Endpointها و زیرساخت اختصاصی — را پوشش می‌دهد. قراردادهای سراسری REST در
[05-api/conventions.md](../05-api/conventions.md)، الگوهای CQRS در
[02-architecture/cqrs-features.md](../02-architecture/cqrs-features.md) و زیرساخت مشترک
(Outbox، کش، قفل توزیع‌شده، Jobها) در [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md)
مستند شده‌اند و در اسناد ماژول تکرار نمی‌شوند. همه شمارش‌ها از فایل‌سیستم مخزن استخراج شده است.

## شناسه و کاربر

| سند | موضوع |
|---|---|
| [identity/auth-security.md](identity/auth-security.md) | ورود با OTP و گوگل، چرخش توکن، سشن‌ها و ابطال آن‌ها |
| [identity/users.md](identity/users.md) | پروفایل کاربر، قفل حساب، آدرس‌ها و داشبورد کاربر |

## کاتالوگ و محتوا

| سند | موضوع |
|---|---|
| [products.md](products.md) | محصول، اسلاگ، وضعیت فعال/ویژه و آمار امتیاز |
| [variants.md](variants.md) | تنوع محصول، SKU، قیمت فروش/اصلی و اتصال به ویژگی و ارسال |
| [attributes.md](attributes.md) | انواع ویژگی و مقادیر ویژگی کاتالوگ |
| [categories.md](categories.md) | دسته‌بندی تخت محصولات و نگهبان حذف |
| [brands.md](brands.md) | برندها با یکتایی نام در دسته و لوگو |
| [media.md](media.md) | رسانه‌های متصل به موجودیت‌ها، ذخیره‌سازی S3 و پویش محتوا |

## فروش

| سند | موضوع |
|---|---|
| [cart.md](cart.md) | سبد خرید کاربر و مهمان، ادغام و همگام‌سازی قیمت |
| [orders.md](orders.md) | سفارش، ماشین وضعیت ۱۲حالته، Saga ثبت سفارش، انقضا و مرجوعی |
| [payments.md](payments.md) | تراکنش پرداخت، درگاه زرین‌پال، وب‌هوک و بازپرداخت |
| [wallet.md](wallet.md) | کیف پول، رزرو/بدهکار/بستانکار، انتقال با OTP، برداشت و تشخیص تقلب |
| [discounts.md](discounts.md) | کدهای تخفیف درصدی/مبلغی/ارسال رایگان و سابقه مصرف |
| [shipping.md](shipping.md) | روش‌های ارسال، هزینه پایه با ضریب واریانت و ارسال رایگان |
| [inventory.md](inventory.md) | موجودی واریانت، انبارها و دفتر فقط-افزودنی حرکت کالا |

## تعامل کاربر

| سند | موضوع |
|---|---|
| [reviews.md](reviews.md) | نظر و امتیاز محصول، تعدیل، پاسخ ادمین و رأی‌گیری |
| [wishlist.md](wishlist.md) | علاقه‌مندی‌های کاربر |
| [support.md](support.md) | تیکت پشتیبانی، پیام‌ها و گذار خودکار وضعیت |
| [notifications.md](notifications.md) | اعلان‌های درون‌برنامه‌ای و ارسال پیامک OTP با Kavenegar |

## پلتفرم

| سند | موضوع |
|---|---|
| [search.md](search.md) | جست‌وجوی Elasticsearch با تحلیلگر فارسی، Outbox اختصاصی و کلیدشکن |
| [analytics.md](analytics.md) | گزارش‌های مدیریتی فقط-خواندنی روی داده سفارش و انبار |
| [audit.md](audit.md) | لاگ حسابرسی با هش یکپارچگی، ماسک داده، آرشیو و نگهداشت |

## جریان‌های میان‌ماژولی که در چند سند به آن‌ها ارجاع می‌شود

| جریان | سند اصلی |
|---|---|
| ثبت سفارش از سبد و ادامه با Saga | [orders.md](orders.md) |
| پرداخت درگاه و بازگشت وب‌هوک | [payments.md](payments.md) |
| رزرو، تأیید و آزادسازی موجودی | [inventory.md](inventory.md) |
| ورود، OTP و مدیریت سشن | [identity/auth-security.md](identity/auth-security.md) |
| ابطال کش واکنشی به رویدادهای دامنه | [02-architecture/cross-cutting.md](../02-architecture/cross-cutting.md) |

## ترتیب پیشنهادی مطالعه

۱. [identity/auth-security.md](identity/auth-security.md) و [identity/users.md](identity/users.md) برای هویت
۲. [products.md](products.md)، [variants.md](variants.md)، [attributes.md](attributes.md)،
   [categories.md](categories.md)، [brands.md](brands.md) برای کاتالوگ
۳. [cart.md](cart.md) → [orders.md](orders.md) → [payments.md](payments.md) → [wallet.md](wallet.md)
   برای زنجیره خرید
۴. [inventory.md](inventory.md)، [discounts.md](discounts.md)، [shipping.md](shipping.md)
   برای پشتیبانی سفارش
۵. بقیه اسناد بسته به نیاز: [reviews.md](reviews.md)، [wishlist.md](wishlist.md)،
   [media.md](media.md)، [notifications.md](notifications.md)، [support.md](support.md)،
   [search.md](search.md)، [analytics.md](analytics.md)، [audit.md](audit.md)
