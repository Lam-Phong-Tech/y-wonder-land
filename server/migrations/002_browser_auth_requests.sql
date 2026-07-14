create table if not exists browser_auth_requests (
    request_id_hash char(64) primary key,
    pkce_challenge varchar(128) not null,
    intent varchar(16) not null default 'login'
        check (intent in ('login', 'register')),
    status varchar(16) not null default 'pending'
        check (status in ('pending', 'approved', 'consumed', 'expired')),
    web_user_id text null,
    web_user_json jsonb not null default '{}'::jsonb,
    expires_at timestamptz not null,
    approved_at timestamptz null,
    consumed_at timestamptz null,
    created_at timestamptz not null default now()
);

create index if not exists idx_browser_auth_requests_expires_at
    on browser_auth_requests(expires_at);

create index if not exists idx_browser_auth_requests_status
    on browser_auth_requests(status);
