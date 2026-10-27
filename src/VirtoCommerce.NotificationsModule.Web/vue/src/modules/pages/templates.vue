<template>
  <VcBlade
    :title="$t('NOTIFICATIONS.PAGES.TEMPLATES.TITLE')"
    :subtitle="displayName(param)"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="50%"
  >
    <VcDataTable
      v-model:selection="selection"
      selection-mode="multiple"
      data-key="key"
      :items="rows"
      :active-item-id="activeItemId"
      :is-row-selectable="(x: Row) => !!x.languageCode && !x.isPredefined && !x.isEdited"
      :empty-state="{ icon: 'lucide-file-code', title: $t('NOTIFICATIONS.PAGES.TEMPLATES.EMPTY') }"
      @row-click="(e: { data: Row }) => open(e.data)"
    >
      <VcColumn
        id="languageCode"
        :title="$t('NOTIFICATIONS.PAGES.TEMPLATES.COLUMNS.LANGUAGE')"
        body-class="ntf-list__name"
        always-visible
      >
        <template #body="{ data }">{{ data.languageCode || $t("NOTIFICATIONS.PAGES.TEMPLATES.DEFAULT_LANGUAGE") }}</template>
      </VcColumn>
      <VcColumn
        id="subject"
        :title="$t(email ? 'NOTIFICATIONS.PAGES.TEMPLATES.COLUMNS.SUBJECT' : 'NOTIFICATIONS.PAGES.TEMPLATES.COLUMNS.MESSAGE')"
      >
        <template #body="{ data }">{{ (email ? data.subject : data.message)?.trim() }}</template>
      </VcColumn>
      <VcColumn
        id="state"
        :title="$t('NOTIFICATIONS.PAGES.TEMPLATES.COLUMNS.STATE')"
      >
        <template #body="{ data }">
          <div class="tw-inline-flex">
            <VcStatus :variant="data.isEdited ? 'warning' : data.isPredefined ? 'info' : 'success'">
              {{ $t(data.isEdited ? "NOTIFICATIONS.TEMPLATE_STATE.EDITED" : data.isPredefined ? "NOTIFICATIONS.TEMPLATE_STATE.PREDEFINED" : "NOTIFICATIONS.TEMPLATE_STATE.CUSTOM") }}
            </VcStatus>
          </div>
        </template>
      </VcColumn>
      <VcColumn
        id="modifiedDate"
        :title="$t('NOTIFICATIONS.PAGES.TEMPLATES.COLUMNS.MODIFIED')"
        type="datetime"
      />
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
import { useAsync, useBlade, useLoading, usePopup, type IBladeToolbar } from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable, VcStatus } from "@vc-shell/framework/ui";
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import {
  displayName,
  effectiveTemplates,
  isEmail,
  loadNotification,
  useNotificationClients,
  withoutTemplate,
  type NotificationEntity,
  type TemplateEntity,
} from "../api";

defineBlade({
  url: "/notification-templates",
  name: "NotificationTemplates",
  permissions: ["notifications:read"],
});

type Row = TemplateEntity & { key: string };

const { t } = useI18n({ useScope: "global" });
const { param, options, openBlade, callParent, exposeToChildren, closeChildren } = useBlade<{ tenantId?: string; tenantType?: string }>();
const { showConfirmation } = usePopup();
const clients = useNotificationClients();

const entity = ref<NotificationEntity>({});
const selection = ref<Row[]>([]);
const activeItemId = ref<string>();
const email = computed(() => isEmail(entity.value.kind));
const rows = computed<Row[]>(() =>
  effectiveTemplates(entity.value.templates).map((x) => ({ ...x, key: x.languageCode || "default" })),
);

const { loading: loadLoading, action: load } = useAsync(async () => {
  entity.value = await loadNotification(await clients.notifications(), param.value!, options.value?.tenantId, options.value?.tenantType);
  selection.value = [];
});

const reloadAll = () => Promise.all([load(), callParent("reload").catch(() => undefined)]);

// the row is the template's language, not the child's param (the notification type): highlighted once it opened
function open(row?: Row) {
  openBlade({
    name: "NotificationTemplateDetails",
    param: param.value,
    // in the URL, so the address of an open template can be shared (?language=..., like the AngularJS ?templateId=)
    query: row ? { language: row.languageCode || "default" } : undefined,
    options: { isNew: !row, ...options.value },
    onOpen: () => (activeItemId.value = row?.key),
    onClose: () => (activeItemId.value = undefined),
  });
}

const { loading: removeLoading, action: removeSelected } = useAsync(async () => {
  const next = selection.value.reduce((acc, x) => withoutTemplate(acc, x.id!), entity.value);
  await (await clients.notifications()).updateNotification(param.value!, next);
  await reloadAll();
});

async function remove() {
  // predefined templates (and their edits) come from the module: they are restored, not deleted;
  // the default-language one is what other languages fall back to, so it can't be deleted (AngularJS deleteList)
  if (!(await showConfirmation(t("NOTIFICATIONS.ALERTS.DELETE_TEMPLATES", { count: selection.value.length })))) return;
  await closeChildren();
  await removeSelected();
}

const loading = useLoading(loadLoading, removeLoading);

const bladeToolbar = ref<IBladeToolbar[]>([
  { id: "refresh", title: t("NOTIFICATIONS.TOOLBAR.REFRESH"), icon: "lucide-refresh-cw", clickHandler: () => load() },
  {
    id: "add",
    title: t("NOTIFICATIONS.TOOLBAR.ADD"),
    icon: "lucide-plus",
    permissions: ["notifications:template:create"],
    clickHandler: () => open(),
  },
  {
    id: "delete",
    title: t("NOTIFICATIONS.TOOLBAR.DELETE"),
    icon: "lucide-trash-2",
    permissions: ["notifications:template:delete"],
    disabled: computed(() => !selection.value.length),
    clickHandler: remove,
  },
]);

onMounted(load);

exposeToChildren({ reload: reloadAll });
</script>
