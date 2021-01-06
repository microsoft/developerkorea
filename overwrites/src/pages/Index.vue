<template>
  <Layout>
      <content-header
        :title="$static.metadata.siteName"
        :sub="$static.metadata.siteDescription"
        image="hero.png">
      </content-header>

      <div class="container mx-auto">
          <div class="flex flex-wrap my-4">

          <FeaturedCard v-if="$page.featured.totalCount>0" :records="$page.featured.edges"/>


          <CardItem v-for="edge in $page.entries.edges" :key="edge.node.id" :record="edge.node" />
        </div>
      </div>
  </Layout>
</template>

<page-query>
  query($page: Int) {
    featured: allBlog(limit: 4, filter: { featured: { eq: true } }, sortBy:"date") {
      totalCount
      edges {
        node {
          id
          title
          description
          image
          path
          timeToRead
          humanTime: date(format: "DD MMM YYYY")
          datetime: date
          category {
            id
            title
            path
          }
          author {
            id
            name
            image(width: 64, height: 64, fit: inside)
            path
          }
        }
      }
    }
    entries: allBlog(perPage: 24, page: $page, sortBy:"date") @paginate {
      totalCount
      pageInfo {
        totalPages
        currentPage
      }
      edges {
        node {
          id
          title
          description
          image
          path
          timeToRead
          featured
          humanTime: date(format: "DD MMM YYYY")
          datetime: date
          category {
            id
            title
            path
          }
          author {
            id
            name
            image(width: 64, height: 64, fit: inside)
            path
          }
        }
      }
    }
  }
</page-query>

<static-query>
query {
  metadata {
    siteName
    siteDescription
  }
}
</static-query>

<script>
import config from '~/.temp/config.js'
import CardItem from "~/components/Content/CardItem.vue";
import FeaturedCard from "~/components/Content/FeaturedCard.vue";
import ContentHeader from "~/components/Partials/ContentHeader.vue";


export default {
  components: {
    CardItem,
    FeaturedCard,
    ContentHeader
  },
  computed: {
    config() {
      return config
    },
    siteUrl () {
      let siteUrl = this.config.siteUrl
      let pathPrefix = this.config.pathPrefix

      return `${siteUrl}${pathPrefix}`
    },
    site() {
      return {
        title: this.$static.metadata.siteName,
        description: this.$static.metadata.siteDescription,
        url: this.siteUrl,
        hero: 'https://raw.githubusercontent.com/microsoft/developerkorea/main/overwrites/src/assets/images/hero.png'
      }
    }
  },
  metaInfo() {
    let metaInfo = {
      title: '🏠',
      meta: [
        {
          key: 'description',
          name: 'description',
          content: this.site.description
        },

        { property: "og:type", content: 'website' },
        { property: "og:title", content: this.site.title },
        { property: "og:description", content: this.site.description },
        { property: "og:url", content: this.site.url },
        { property: "og:image", content: this.site.hero },

        { name: "twitter:card", content: 'summary_large_image' },
        { name: "twitter:site", content:  '@microsofttechKR'},
        { name: "twitter:creator", content: '@microsofttechKR' },
        { name: "twitter:title", content: this.site.title },
        { name: "twitter:description", content: this.site.description },
        { name: "twitter:image", content: this.site.hero },
      ],
      link: []
    }

    return metaInfo
  }
};
</script>
