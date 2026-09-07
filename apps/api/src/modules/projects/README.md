# Projects and contracting pack (Phase 21)

Feature flag: `tenant_settings.pack.projects`. Disabled tenants receive `404` for `/projects/*`.

Endpoints cover project creation, stage templates, per-user stage accreditation, BOQ maintenance, progress bill calculation/posting, retention release and lightweight requirements. Progress bills compute work value, previous/cumulative values, retention and net due from BOQ terms. Posting creates a normal sale invoice through the zero-inventory/service path and can also post accounting lines if the caller supplies account ids/fiscal period.
