<template>
  <VcBlade
    :title="title"
    :subtitle="displayName(param)"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="80%"
  >
    <VcContainer>
      <div class="ntf-form ntf-template">
        <VcForm class="ntf-template__editor tw-space-y-4">
          <VcRow class="tw-gap-4">
            <VcCol>
              <VcSelect
                v-if="isNew"
                v-model="item.languageCode"
                :label="$t('NOTIFICATIONS.PAGES.TEMPLATE.FIELDS.LANGUAGE')"
                :options="freeLanguages"
                :option-value="(x: string) => x"
                :option-label="(x: string) => x"
                :error-message="languageError"
                required
              />
              <VcField
                v-else
                :label="$t('NOTIFICATIONS.PAGES.TEMPLATE.FIELDS.LANGUAGE')"
                :model-value="item.languageCode || $t('NOTIFICATIONS.PAGES.TEMPLATES.DEFAULT_LANGUAGE')"
              />
            </VcCol>
            <VcCol v-if="email && layouts.length">
              <VcSelect
                v-model="item.notificationLayoutId"
                :label="$t('NOTIFICATIONS.PAGES.TEMPLATE.FIELDS.LAYOUT')"
                :options="layouts"
                option-label="name"
                :placeholder="$t('NOTIFICATIONS.PAGES.TEMPLATE.FIELDS.DEFAULT_LAYOUT')"
              />
            </VcCol>
          </VcRow>
          <template v-if="email">
            <VcInput
              v-model="item.subject"
              :label="$t('NOTIFICATIONS.PAGES.TEMPLATE.FIELDS.SUBJECT')"
              required
            />
            <!-- Liquid + HTML: a monospace textarea keeps the markup exactly as typed -->
            <VcTextarea
              v-model="item.body"
              class="ntf-code ntf-code--body"
              :label="$t('NOTIFICATIONS.PAGES.TEMPLATE.FIELDS.BODY')"
              required
            />
          </template>
          <VcTextarea
            v-else
            v-model="item.message"
            class="ntf-code"
            :label="$t('NOTIFICATIONS.PAGES.TEMPLATE.FIELDS.MESSAGE')"
            required
          />
          <VcTextarea
            v-model="item.sample"
            class="ntf-code ntf-code--sample"
            :label="$t('NOTIFICATIONS.PAGES.TEMPLATE.FIELDS.SAMPLE')"
            :hint="$t('NOTIFICATIONS.PAGES.TEMPLATE.SAMPLE_HINT')"
            :error-message="sampleError"
          />
        </VcForm>

        <VcCard
          v-if="email"
          class="ntf-template__preview"
          :header="$t('NOTIFICATIONS.PAGES.TEMPLATE.PREVIEW')"
        >
          <div
            v-if="previewError"
            class="ntf-preview__error"
          >
            {{ previewError }}
          </div>
          <!-- sandboxed: the e-mail's own styles and scripts stay out of the shell -->
          <iframe
            v-else
            class="ntf-preview"
            sandbox=""
            :srcdoc="previewHtml"
            :title="$t('NOTIFICATIONS.PAGES.TEMPLATE.PREVIEW')"
          />
        </VcCard>
      </div>
    </VcContainer>

    <VcPopup
      v-if="sendDialog"
      :model-value="true"
      :title="$t('NOTIFICATIONS.PAGES.TEMPLATE.SEND_TEST')"
      @update:model-value="sendDialog = false"
    >
      <template #content>
        <VcInput
          v-model="testEmail"
          type="email"
          :label="$t('NOTIFICATIONS.PAGES.TEMPLATE.SEND_TO')"
          required
        />
      </template>
      <template #footer>
        <div class="ntf-popup-footer">
          <VcButton
            variant="secondary"
            @click="sendDialog = false"
          >
            {{ $t("NOTIFICATIONS.TOOLBAR.CANCEL") }}
          </VcButton>
          <VcButton
            :disabled="!testEmail"
            @click="sendTest()"
          >
            {{ $t("NOTIFICATIONS.PAGES.TEMPLATE.SEND") }}
          </VcButton>
        </div>
      </template>
    </VcPopup>
  </VcBlade>
</template>

<script setup lang="ts">
import { useAsync, useBlade, useBladeForm, useFunctions, useLoading, usePopup, useUser, notification, type IBladeToolbar } from "@vc-shell/framework";
import {
  VcBlade,
  VcButton,
  VcCard,
  VcCol,
  VcContainer,
  VcField,
  VcForm,
  VcInput,
  VcPopup,
  VcRow,
  VcSelect,
  VcTextarea,
} from "@vc-shell/framework/ui";
import { computed, onMounted, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import type { NotificationLayout } from "../../api_client/virtocommerce.notifications";
import {
  currentUserEmail,
  displayName,
  effectiveTemplates,
  isEmail,
  loadLanguages,
  loadNotification,
  renderTemplate,
  toSaveModel,
  useNotificationClients,
  withTemplate,
  withoutTemplate,
  type NotificationEntity,
  type TemplateEntity,
} from "../api";

defineBlade({
  url: "/notification-template",
  name: "NotificationTemplateDetails",
  permissions: ["notifications:read"],
});

const { t } = useI18n({ useScope: "global" });
const { param, options, query, closeSelf, callParent } = useBlade<{
  isNew?: boolean;
  tenantId?: string;
  tenantType?: string;
}>();
const { showConfirmation } = usePopup();
const { debounce } = useFunctions();
const { user } = useUser();
const clients = useNotificationClients();

const notificationEntity = ref<NotificationEntity>({});
const item = ref<TemplateEntity>({});
const isNew = ref(!!options.value?.isNew);
const languages = ref<string[]>([]);
const layouts = ref<NotificationLayout[]>([]);
const previewHtml = ref("");
const previewError = ref<string>();
const sendDialog = ref(false);
const testEmail = ref("");

const form = useBladeForm({ data: item, closeConfirmMessage: computed(() => t("NOTIFICATIONS.ALERTS.CLOSE_CONFIRMATION")) });

const email = computed(() => isEmail(notificationEntity.value.kind));
const usedLanguages = computed(() => effectiveTemplates(notificationEntity.value.templates).map((x) => x.languageCode ?? null));
const freeLanguages = computed(() => languages.value.filter((x) => !usedLanguages.value.includes(x)));
const languageError = computed(() =>
  isNew.value && item.value.languageCode && usedLanguages.value.includes(item.value.languageCode)
    ? t("NOTIFICATIONS.VALIDATION.LANGUAGE_EXISTS")
    : undefined,
);
const title = computed(() =>
  isNew.value ? t("NOTIFICATIONS.PAGES.TEMPLATE.TITLE_NEW") : t("NOTIFICATIONS.PAGES.TEMPLATE.TITLE", { language: item.value.languageCode || t("NOTIFICATIONS.PAGES.TEMPLATES.DEFAULT_LANGUAGE") }),
);

/** the sample data; undefined while it isn't valid JSON */
const sample = computed<object | undefined>(() => {
  if (!item.value.sample?.trim()) return {};
  try {
    return JSON.parse(item.value.sample);
  } catch {
    return undefined;
  }
});
const sampleError = computed(() => (sample.value ? undefined : t("NOTIFICATIONS.VALIDATION.SAMPLE_JSON")));
const valid = computed(
  () => !sampleError.value && !languageError.value && (email.value ? !!item.value.subject && !!item.value.body : !!item.value.message) && (!isNew.value || !!item.value.languageCode),
);

/**
 * the data the template is rendered with: the notification itself (templates included: the server picks the
 * language's template from them, and Send test renders the saved ones) plus the sample, as the AngularJS editor does
 */
function renderData() {
  return { ...notificationEntity.value, ...sample.value };
}

// the languages and layouts to pick from: a save doesn't change them, so they are read once
async function loadLookups() {
  const layoutsClient = await clients.layouts();
  const [langs, layoutResult] = await Promise.all([
    loadLanguages().catch(() => []),
    layoutsClient.searchNotificationLayouts({ take: 100 }).catch(() => undefined),
  ]);
  languages.value = langs;
  layouts.value = layoutResult?.results ?? [];
}

const { loading: loadLoading, action: load } = useAsync(async () => {
  notificationEntity.value = await loadNotification(await clients.notifications(), param.value!, options.value?.tenantId, options.value?.tenantType);
  if (!isNew.value) {
    // which template: ?templateId=<id> (the AngularJS deep link) or ?language=<code|default>;
    // neither = the default-language one
    const templates = effectiveTemplates(notificationEntity.value.templates);
    const templateId = query.value?.templateId;
    const language = query.value?.language && query.value.language !== "default" ? query.value.language : null;
    const found = templateId
      ? (templates.find((x) => x.id === templateId) ??
        notificationEntity.value.templates?.find((x) => x.id === templateId))
      : templates.find((x) => (x.languageCode ?? null) === language);
    item.value = { ...(found ?? {}) };
  } else {
    // a new template starts from the default one, like a copy to translate
    const base = effectiveTemplates(notificationEntity.value.templates).find((x) => !x.languageCode);
    item.value = {
      subject: base?.subject,
      body: base?.body,
      message: base?.message,
      sample: base?.sample,
      notificationLayoutId: base?.notificationLayoutId,
    };
  }
  form.setBaseline();
});

async function refreshPreview() {
  if (!email.value || !item.value.body || !sample.value) return;
  try {
    previewError.value = undefined;
    previewHtml.value = await renderTemplate(
      param.value!,
      item.value.languageCode ?? undefined,
      item.value.body,
      renderData(),
      item.value.notificationLayoutId,
    );
  } catch (e) {
    previewError.value = String(e);
  }
}

// also renders the preview once a load has set the template
watch(
  () => [item.value.body, item.value.sample, item.value.notificationLayoutId],
  debounce(refreshPreview, 500),
);

const { loading: saveLoading, action: persist } = useAsync<NotificationEntity>(async (next) => {
  await (await clients.notifications()).updateNotification(param.value!, toSaveModel(next!));
  form.setBaseline();
});

// after a save: this template and the parent blades take the server's version
const reloadAll = () => Promise.all([load(), callParent("reload").catch(() => undefined)]);

async function save() {
  if (!valid.value) return;
  await persist(withTemplate(notificationEntity.value, item.value));
  if (isNew.value) {
    await callParent("reload").catch(() => undefined);
    await closeSelf();
  } else await reloadAll();
}

/** drops the override of a predefined template: the module's own template is used again */
async function restore() {
  if (!(await showConfirmation(t("NOTIFICATIONS.ALERTS.RESTORE_TEMPLATE")))) return;
  await persist(withoutTemplate(notificationEntity.value, item.value.id!));
  await reloadAll();
}

const { loading: sendLoading, action: sendTest } = useAsync(async () => {
  sendDialog.value = false;
  const data = { ...renderData(), cc: undefined, bcc: undefined, languageCode: item.value.languageCode || "default", to: testEmail.value };
  await (await clients.notifications()).sharePreview(item.value.languageCode || "default", param.value!, {
    text: item.value.body,
    data,
    notificationLayoutId: item.value.notificationLayoutId,
  });
  notification.success(t("NOTIFICATIONS.ALERTS.TEST_SENT", { email: testEmail.value }));
});

const loading = useLoading(loadLoading, saveLoading, sendLoading);

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "save",
    title: computed(() => t(isNew.value ? "NOTIFICATIONS.TOOLBAR.CREATE" : "NOTIFICATIONS.TOOLBAR.SAVE")),
    icon: "lucide-save",
    permissions: ["notifications:update"],
    disabled: computed(() => !valid.value || (!isNew.value && !form.isModified.value)),
    clickHandler: save,
  },
  {
    id: "reset",
    title: t("NOTIFICATIONS.TOOLBAR.RESET"),
    icon: "lucide-undo-2",
    isVisible: computed(() => !isNew.value),
    disabled: computed(() => !form.isModified.value),
    clickHandler: () => form.revert(),
  },
  {
    id: "preview",
    title: t("NOTIFICATIONS.TOOLBAR.PREVIEW"),
    icon: "lucide-eye",
    isVisible: email,
    disabled: computed(() => !!sampleError.value),
    clickHandler: refreshPreview,
  },
  {
    id: "sendTest",
    title: t("NOTIFICATIONS.PAGES.TEMPLATE.SEND_TEST"),
    icon: "lucide-send",
    permissions: ["notifications:templates:read"],
    isVisible: email,
    disabled: computed(() => !!sampleError.value || !item.value.body),
    clickHandler: async () => {
      testEmail.value = await currentUserEmail(user.value?.userName);
      sendDialog.value = true;
    },
  },
  {
    id: "restore",
    title: t("NOTIFICATIONS.TOOLBAR.RESTORE"),
    icon: "lucide-history",
    permissions: ["notifications:template:delete"],
    isVisible: computed(() => !isNew.value && !!item.value.isEdited),
    clickHandler: restore,
  },
]);

onMounted(() => Promise.all([loadLookups(), load()]));
</script>

<style lang="scss">
.ntf-template {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  gap: 16px;
  align-items: start;
}

.ntf-code--sample textarea {
  min-height: 160px;
}

.ntf-preview__error {
  padding: 16px;
  color: var(--color-danger-500, #ef4444);
  white-space: pre-wrap;
  font-size: 13px;
}
</style>
