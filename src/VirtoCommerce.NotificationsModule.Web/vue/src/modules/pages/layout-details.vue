<template>
  <VcBlade
    :title="isNew ? $t('NOTIFICATIONS.PAGES.LAYOUT.TITLE_NEW') : item.name"
    :subtitle="$t('NOTIFICATIONS.PAGES.LAYOUT.SUBTITLE')"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="60%"
  >
    <VcContainer>
      <VcForm class="ntf-form tw-space-y-4">
        <VcRow class="tw-gap-4">
          <VcCol>
            <VcInput
              v-model="item.name"
              :label="$t('NOTIFICATIONS.PAGES.LAYOUTS.COLUMNS.NAME')"
              :disabled="item.isPredefined"
              required
            />
          </VcCol>
          <VcCol>
            <VcSwitch
              v-model="item.isDefault"
              :label="$t('NOTIFICATIONS.PAGES.LAYOUT.IS_DEFAULT')"
            />
          </VcCol>
        </VcRow>
        <!-- Liquid + HTML: a monospace textarea keeps the markup exactly as typed -->
        <VcTextarea
          v-model="item.template"
          class="ntf-code ntf-code--body"
          :label="$t('NOTIFICATIONS.PAGES.LAYOUT.TEMPLATE')"
        />
      </VcForm>
    </VcContainer>
  </VcBlade>
</template>

<script setup lang="ts">
import { useAsync, useBlade, useBladeForm, useLoading, usePopup, type IBladeToolbar } from "@vc-shell/framework";
import { VcBlade, VcCol, VcContainer, VcForm, VcInput, VcRow, VcSwitch, VcTextarea } from "@vc-shell/framework/ui";
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import type { NotificationLayout } from "../../api_client/virtocommerce.notifications";
import { useNotificationClients } from "../api";

defineBlade({
  url: "/notification-layout",
  name: "NotificationLayoutDetails",
  permissions: ["notifications:read"],
});

const { t } = useI18n({ useScope: "global" });
const { param, closeSelf, callParent } = useBlade();
const { showConfirmation } = usePopup();
const clients = useNotificationClients();

const item = ref<NotificationLayout>({ isDefault: false, template: "" });
const id = ref(param.value);
const isNew = computed(() => !id.value);
/** a module-provided layout that was edited: "reset to default" brings the module's version back */
const overridden = computed(() => !!item.value.isPredefined && item.value.id !== item.value.name);

const form = useBladeForm({ data: item, closeConfirmMessage: computed(() => t("NOTIFICATIONS.ALERTS.CLOSE_CONFIRMATION")) });

const reloadParent = () => callParent("reload").catch(() => undefined);

const { loading: loadLoading, action: load } = useAsync(async () => {
  if (id.value) item.value = await (await clients.layouts()).getNotificationLayoutById(id.value);
  form.setBaseline();
});

const { loading: saveLoading, action: save } = useAsync(async () => {
  const client = await clients.layouts();
  const wasNew = isNew.value;
  const saved = wasNew ? await client.createNotificationLayout(item.value) : await client.updateNotificationLayout(item.value);
  id.value = saved?.id ?? id.value;
  form.setBaseline();
  if (wasNew) {
    await reloadParent();
    await closeSelf();
  } else await Promise.all([load(), reloadParent()]);
});

const { loading: resetLoading, action: reset } = useAsync(async () => {
  await (await clients.layouts()).resetNotificationLayoutToDefault(item.value.id!);
  // the module's layout is addressed by its name again
  id.value = item.value.name;
  await Promise.all([load(), reloadParent()]);
});

const { loading: deleteLoading, action: deleteLayout } = useAsync(async () => {
  await (await clients.layouts()).deleteNotificationLayout([item.value.id!]);
  form.setBaseline();
  await reloadParent();
  await closeSelf();
});

const loading = useLoading(loadLoading, saveLoading, resetLoading, deleteLoading);

async function resetToDefault() {
  if (await showConfirmation(t("NOTIFICATIONS.ALERTS.RESET_LAYOUT"))) await reset();
}

async function remove() {
  if (await showConfirmation(t("NOTIFICATIONS.ALERTS.DELETE_LAYOUTS", { count: 1 }))) await deleteLayout();
}

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "save",
    title: computed(() => t(isNew.value ? "NOTIFICATIONS.TOOLBAR.CREATE" : "NOTIFICATIONS.TOOLBAR.SAVE")),
    icon: "lucide-save",
    permissions: ["notifications:update"],
    disabled: computed(() => !item.value.name || (!isNew.value && !form.isModified.value)),
    clickHandler: () => save(),
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
    id: "resetDefault",
    title: t("NOTIFICATIONS.PAGES.LAYOUT.RESET_DEFAULT"),
    icon: "lucide-history",
    permissions: ["notifications:update"],
    isVisible: overridden,
    clickHandler: resetToDefault,
  },
  {
    id: "delete",
    title: t("NOTIFICATIONS.TOOLBAR.DELETE"),
    icon: "lucide-trash-2",
    permissions: ["notifications:delete"],
    isVisible: computed(() => !isNew.value && !item.value.isPredefined),
    clickHandler: remove,
  },
]);

onMounted(load);
</script>
