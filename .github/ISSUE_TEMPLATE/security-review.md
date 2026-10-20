---
name: Security review
about: بازبینی دوره‌ای امنیت (فاز ۶ ممیزی) — هر ۶ ماه یا پیش از انتشار Major
title: 'Security review: <YYYY-MM>'
labels: security
assignees: ''
---

## هدف

بازبینی دوره‌ای ده اصل امنیتی فاز ۶ و بدهی‌های باقی‌ماندهٔ ممیزی.
مرجع: [`docs/07-operations/security-invariants.md`](../docs/07-operations/security-invariants.md)

## چک‌لیست

- [ ] `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/security/verify-security.ps1` پاس می‌شود
      (به‌جز PENDING تاریخچه، اگر بازنویسی انجام نشده).
- [ ] job هفتگی `secret-history-scan` در CI سبز است (تاریخچه پاک است).
- [ ] `dependency-audit` سبز است و PRهای Dependabot بررسی شده‌اند.
- [ ] کلید JWT، OTP Pepper و کلیدهای سرویس‌های بیرونی در بازهٔ ۹۰–۱۸۰ روز گذشته چرخش کرده‌اند.
- [ ] `InitialAdmin__PhoneNumbers` در Production خالی است.
- [ ] Swagger در Production خاموش است و `PaymentGateway__EnableMockGateway=false` است.
- [ ] مدیر امنیتی/مالک فنی کد مسیرهای حساس (Auth، Wallet، Payment، Admin) را بازبینی کرده است.
- [ ] تست‌های فاز ۶ بدون تغییر یا با تغییر مستند شده‌اند:
      `dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Tests.Security."`
- [ ] فهرست فاز ۶ در گزارش ممیزی با کد فعلی مطابق است.
- [ ] یافتهٔ جدید: ... (در صورت وجود، شماره SEC جدید اضافه شود)

## یادداشت‌ها

<!-- نتیجهٔ بازبینی، تصمیم‌ها و اقدام‌های پیگیری -->
