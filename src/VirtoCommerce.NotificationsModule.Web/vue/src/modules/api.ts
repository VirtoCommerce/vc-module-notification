import { SecurityClient, useApiClient } from "@vc-shell/framework";
import {
  NotificationLayoutsClient,
  NotificationsClient,
  type Notification,
  type NotificationMessage,
  type NotificationTemplate,
} from "../api_client/virtocommerce.notifications";

// The generated types are the base classes; the API sends the Email/Sms descendants.
export interface NotificationEntity extends Notification {
  from?: string;
  to?: string;
  cc?: string[];
  bcc?: string[];
  replyTo?: string;
  number?: string;
  templates?: TemplateEntity[];
}

export interface TemplateEntity extends NotificationTemplate {
  subject?: string;
  body?: string;
  message?: string;
  sample?: string;
  notificationLayoutId?: string;
  /** client-side: a predefined template the user overrode (as in the AngularJS template list) */
  isEdited?: boolean;
}

export interface MessageEntity extends NotificationMessage {
  from?: string;
  to?: string;
  cc?: string[];
  bcc?: string[];
  replyTo?: string;
  subject?: string;
  body?: string;
  number?: string;
  message?: string;
}

export const isEmail = (kind?: string) => kind === "EmailNotification";

export function useNotificationClients() {
  return {
    notifications: useApiClient(NotificationsClient).getApiClient,
    layouts: useApiClient(NotificationLayoutsClient).getApiClient,
  };
}

/** The platform's languages (settings/values: any signed-in user, like legacy settings.getValues), read once per session */
let languages: Promise<string[]> | undefined;
export function loadLanguages() {
  languages ??= fetch("/api/platform/settings/values/VirtoCommerce.Core.General.Languages")
    .then(async (res) => {
      if (!res.ok) throw new Error(`languages: HTTP ${res.status}`);
      return (((await res.json()) as unknown[] | null) ?? []).map(String);
    })
    .catch((e) => {
      languages = undefined; // a failed read is tried again next time
      throw e;
    });
  return languages;
}

/** A notification as a tenant (store) sees it: with no tenant-level one yet, saving it creates one for the tenant. */
export async function loadNotification(client: NotificationsClient, type: string, tenantId?: string, tenantType?: string) {
  const data = (await client.getNotificationByTypeId(type, tenantId, tenantType)) as NotificationEntity;
  if (data.tenantIdentity?.isEmpty && tenantId && tenantType) data.tenantIdentity = { id: tenantId, type: tenantType };
  return data;
}

/** "OrderCreateEmailNotification" -> "Order create email notification" (the AngularJS list shows the type) */
export const displayName = (type?: string) =>
  (type ?? "").replace(/([a-z0-9])([A-Z])/g, "$1 $2").replace(/^./, (c) => c.toUpperCase()).replace(/ (\S)/g, (m) => m.toLowerCase());

/**
 * Renders a template body against sample data (the endpoint the AngularJS preview uses).
 * Hand-written: the generated renderingTemplate() drops the response body.
 */
export async function renderTemplate(type: string, language: string | undefined, text: string, data: object, notificationLayoutId?: string) {
  const res = await fetch(
    `/api/notifications/${encodeURIComponent(type)}/templates/${encodeURIComponent(language || "default")}/rendercontent`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ text, data, notificationLayoutId }),
    },
  );
  if (!res.ok) throw new Error(`${res.status}: ${await res.text()}`);
  return ((await res.json()) as { html?: string }).html ?? "";
}

/**
 * One template per language: an override wins over the predefined (file) template it edits, and is
 * flagged `isEdited` (same rule as the AngularJS getEffectiveTemplateListByLanguage).
 */
export function effectiveTemplates(templates: TemplateEntity[] = []) {
  const groups = new Map<string, TemplateEntity[]>();
  for (const x of templates) {
    const key = x.languageCode ?? "";
    groups.set(key, [...(groups.get(key) ?? []), x]);
  }
  return [...groups.values()].map((group) => {
    const override = group.find((x) => !x.isPredefined);
    const hasPredefined = group.some((x) => x.isPredefined);
    return override ? { ...override, isEdited: hasPredefined } : group[0];
  });
}

/**
 * The notification to PUT with `template` saved as a database template (the server never stores
 * predefined templates; any database template left out of the list is deleted).
 */
export function withTemplate(notification: NotificationEntity, template: TemplateEntity): NotificationEntity {
  const { isEdited: _edited, ...saved } = template;
  const others = (notification.templates ?? []).filter(
    (x) => !(template.id ? x.id === template.id : !x.isPredefined && (x.languageCode ?? null) === (template.languageCode ?? null)),
  );
  return { ...notification, templates: [...others, { ...saved, isPredefined: false }] };
}

/** The notification without the database template `id` (Restore / delete). */
export function withoutTemplate(notification: NotificationEntity, id: string): NotificationEntity {
  return { ...notification, templates: (notification.templates ?? []).filter((x) => x.id !== id) };
}

/** What the API accepts on save (persistNotification in the AngularJS editor): no read-only templates. */
export function toSaveModel(notification: NotificationEntity): NotificationEntity {
  return {
    ...notification,
    templates: (notification.templates ?? []).filter((x) => !x.isReadonly).map(({ isEdited: _edited, ...x }) => x),
  };
}

/** The signed-in user's e-mail (the default recipient of a test e-mail, as in the AngularJS editor). */
export async function currentUserEmail(userName?: string) {
  if (!userName) return "";
  return (await useApiClient(SecurityClient).getApiClient()).getUserByName(userName).then(
    (user) => user.email ?? "",
    () => "",
  );
}
